using System.Linq.Expressions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Services.Billing.Contracts;
using Core.Services.Billing.Exceptions;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.Billing;

/// <summary>
/// Admin-facing management of the subscription packages (plan extras) shown in
/// the app's paywall. Prices are kept in tiyn on the entity but exchanged in
/// so'm (UZS) over the API, matching <see cref="GetPlanExtras"/>.
/// </summary>
[Injectable]
public class PlanExtraService(AppDbContext dbContext)
{
    public async Task<Wrapper> GetAll(DataQueryRequest q)
    {
        return await dbContext.PlanExtras
            .OrderBy(x => x.Plan)
            .ThenBy(x => x.DurationInMonths)
            .Select(Projection)
            .GetByDataQueryAsync(q);
    }

    public async Task<GetPlanExtras> GetById(long id)
    {
        return await dbContext.PlanExtras
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(Projection)
            .FirstOrDefaultAsync()
               ?? throw new PlanExtraNotFoundException();
    }

    public async Task<GetPlanExtras> CreateOrUpdate(CreateOrUpdatePlanExtraDto dto)
    {
        // 0 means "no discount"; otherwise the crossed-out price must be higher.
        if (dto.OriginalFee != 0 && dto.OriginalFee <= dto.Fee)
            throw new PlanExtraFeeInvalidException();

        var planExtra = dto.Id.HasValue
            ? await dbContext.PlanExtras.FirstOrDefaultAsync(x => x.Id == dto.Id.Value)
              ?? throw new PlanExtraNotFoundException()
            : new PlanExtra();

        if (dto.IsActive)
        {
            var duplicateExists = await dbContext.PlanExtras.AnyAsync(x =>
                x.Id != planExtra.Id
                && x.Plan == dto.Plan
                && x.DurationInMonths == dto.Duration
                && x.IsActive);

            if (duplicateExists)
                throw new PlanExtraDurationAlreadyExistsException();
        }

        planExtra.Plan = dto.Plan;
        planExtra.DurationInMonths = dto.Duration;
        planExtra.Fee = ToTiyn(dto.Fee);
        planExtra.OriginalFee = ToTiyn(dto.OriginalFee);
        planExtra.IsActive = dto.IsActive;
        // An inactive package must never be advertised as the best offer.
        planExtra.IsPopular = dto.IsPopular && dto.IsActive;

        if (planExtra.Id == 0)
            dbContext.PlanExtras.Add(planExtra);

        await dbContext.SaveChangesAsync();

        if (planExtra.IsPopular)
            await ClearPopularOnOthers(planExtra);

        return ToDto(planExtra);
    }

    public async Task Delete(long id)
    {
        var planExtra = await dbContext.PlanExtras.FirstOrDefaultAsync(x => x.Id == id)
                        ?? throw new PlanExtraNotFoundException();

        // subscription_orders.plan_extra_id is Restrict — keep purchase history intact.
        if (await dbContext.SubscriptionOrders.AnyAsync(x => x.PlanExtraId == id))
            throw new PlanExtraInUseException();

        dbContext.PlanExtras.Remove(planExtra);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>Only one package per plan may be the "best offer".</summary>
    private async Task ClearPopularOnOthers(PlanExtra planExtra)
    {
        await dbContext.PlanExtras
            .Where(x => x.Plan == planExtra.Plan && x.Id != planExtra.Id && x.IsPopular)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPopular, false));
    }

    private static long ToTiyn(double som) => (long)Math.Round(som * 100);

    private static readonly Expression<Func<PlanExtra, GetPlanExtras>> Projection = x => new GetPlanExtras
    {
        Id = x.Id,
        Plan = x.Plan,
        Duration = x.DurationInMonths,
        Fee = x.Fee / 100d,
        OriginalFee = x.OriginalFee / 100d,
        IsActive = x.IsActive,
        IsPopular = x.IsPopular,
        CreatedAt = x.CreatedAt
    };

    private static GetPlanExtras ToDto(PlanExtra x) => new()
    {
        Id = x.Id,
        Plan = x.Plan,
        Duration = x.DurationInMonths,
        Fee = x.Fee / 100d,
        OriginalFee = x.OriginalFee / 100d,
        IsActive = x.IsActive,
        IsPopular = x.IsPopular,
        CreatedAt = x.CreatedAt
    };
}
