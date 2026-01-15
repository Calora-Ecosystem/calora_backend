using BRB.Core.Common.Exceptions;
using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Enums;
using Core.Services.Auth;
using Core.Services.Billing.Click;
using Core.Services.Billing.Contracts;
using Core.Services.Billing.Payme;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResultWrapper.Library;

namespace Core.Services.Billing;

[Injectable]
public class OrderService(AppDbContext dbContext, IServiceProvider serviceProvider, AuthService authService)
{
    public async Task<string> CreateSubscriptionOrder(long userId, CreateSubscriptionOrderDto dto)
    {
        await dbContext.Users.ExistsOrThrowsNotFoundException(userId);
        var planExtra = await dbContext.PlanExtras.GetByIdOrThrowsNotFoundException(dto.PlanExtraId);

        if (await dbContext.Orders
                .AnyAsync(x => x.UserId == userId
                               && x.Type == EnumOrderType.Subscription
                               && x.Status == EnumOrderStatus.Pending))
            throw new BadRequestException("Pending subscription order already exists");

        Order order = null!;

        await dbContext.Transactional(async () =>
        {
            order = new Order()
            {
                Amount = planExtra.Fee,
                Type = EnumOrderType.Subscription,
                Provider = dto.Provider,
                UserId = userId,
                Status = EnumOrderStatus.Pending,
            };

            dbContext.Add(order);
            await dbContext.SaveChangesAsync();

            var subscriptionOrder = new SubscriptionOrder()
            {
                OrderId = order.Id,
                Plan = planExtra.Plan,
                PlanExtraId = planExtra.Id
            };

            dbContext.Add(subscriptionOrder);

            await (dto.Provider switch
            {
                EnumPaymentProviders.Click => serviceProvider.GetRequiredService<ClickService>()
                    .CreateTransaction(order),
                EnumPaymentProviders.Payme => serviceProvider.GetRequiredService<PaymeService>()
                    .CreateInternalTransaction(order),
                _ => throw new Exception("Provider not found")
            });

            await dbContext.SaveChangesAsync();
        });

        return await this.MakePaymentLink(order.UserId, order.Id);
    }

    public async Task<Wrapper> GetOrders(DataQueryRequest q, long? userId = null)
    {
        var query = dbContext.Orders.AsQueryable();

        if (userId is not null) query = query.Where(x => x.UserId == userId);

        return await query
            .Select(x => new GetOrdersDto
            {
                Id = x.Id, Status = x.Status, Amount = x.Amount,
                Type = x.Type,
                Provider = x.Provider,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .GetByDataQueryAsync(q);
    }

    public async Task Remove(long userId, long orderId)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(x =>
                        x.Id == orderId && x.UserId == userId && x.Status == EnumOrderStatus.Pending)
                    ?? throw new NotFoundException("Order not found");

        dbContext.Orders.Remove(order);
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> AcceptPaymentAsync(long orderId)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.Id == orderId);

        if (order is null) return false;

        var result = await (order.Type switch
        {
            EnumOrderType.Subscription => AcceptSubscriptionPaymentAsync(order),
            _ => throw new Exception("Order type not found")
        });

        order.Status = result ? EnumOrderStatus.Confirmed : EnumOrderStatus.Canceled;
        await dbContext.SaveChangesAsync();

        return result;
    }

    private async Task<bool> AcceptSubscriptionPaymentAsync(Order order)
    {
        var orderExtra = await dbContext.SubscriptionOrders
            .Include(subscriptionOrder => subscriptionOrder.PlanExtra)
            .FirstOrDefaultAsync(x => x.OrderId == order.Id);

        if (orderExtra is null) return false;

        var now = DateTime.Now;

        var planExtra = await dbContext.PlanExtras.FirstOrDefaultAsync(x => x.Plan == orderExtra.Plan && x.IsActive);

        if (planExtra is null) return false;

        await authService.KillAllUserSessions(order.UserId);

        var subscription = new Subscription()
        {
            SubscriptionPlan = orderExtra.Plan,
            UserId = order.UserId,
            StartsAt = now,
            EndsAt = now.AddMonths(orderExtra.PlanExtra.DurationInMonths),
            IsActive = true,
        };

        dbContext.Add(subscription);
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<string> MakePaymentLink(long userId, long orderId)
    {
        var order = await dbContext.Orders
                        .FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId)
                    ?? throw new NotFoundException("Order not found");

        return await (order.Provider switch
        {
            EnumPaymentProviders.Click => serviceProvider.GetRequiredService<ClickService>()
                .MakeClickPaymentLink(order.Id, order.Amount),
            EnumPaymentProviders.Payme => serviceProvider.GetRequiredService<PaymeService>()
                .MakeClickPaymentLink(order.Id, order.Amount),
            _ => throw new Exception("Provider not found")
        });
    }

    public async Task<Wrapper> GetPlanExtras(EnumSPlans plan, DataQueryRequest q)
    {
        return await dbContext
            .PlanExtras
            .Where(x => x.Plan == plan && x.IsActive)
            .Select(x => new GetPlanExtras
            {
                Id = x.Id, Duration = x.DurationInMonths, IsActive = x.IsActive,
                Plan = x.Plan,
                CreatedAt = x.CreatedAt
            })
            .GetByDataQueryAsync(q);
    }
}