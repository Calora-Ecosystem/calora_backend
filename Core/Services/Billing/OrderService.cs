using BRB.Core.Common.Models;
using Core.Services.Billing.Exceptions;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Entities.Billing.Payme;
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
public class OrderService(
    AppDbContext dbContext,
    IServiceProvider serviceProvider,
    AuthService authService,
    CouponService couponService)
{
    public async Task<CreateSubscriptionOrderResponseDto> CreateSubscriptionOrder(long userId,
        CreateSubscriptionOrderDto dto)
    {
        await dbContext.Users.ExistsOrThrowsNotFoundException(userId);

        if (await dbContext.Subscriptions.AnyAsync(x => x.UserId == userId && x.IsActive))
            throw new UserAlreadySubscribedException();

        var planExtra = await dbContext.PlanExtras.GetByIdOrThrowsNotFoundException(dto.PlanExtraId);

        if (await dbContext.Orders
                .AnyAsync(x => x.UserId == userId
                               && x.Type == EnumOrderType.Subscription
                               && x.Status == EnumOrderStatus.Pending))
            throw new PendingOrderAlreadyExistsException();
        
        if (dto.CouponId.HasValue)
            await dbContext.Coupons.ExistsOrThrowsNotFoundException(dto.CouponId.Value);

        Order order = null!;
        bool paymentRequired = true;

        await dbContext.Transactional(async () =>
        {
            order = new Order()
            {
                Amount = planExtra.Fee,
                Type = EnumOrderType.Subscription,
                Provider = dto.Provider,
                UserId = userId,
                Status = EnumOrderStatus.Pending,
                CouponId = dto.CouponId
            };

            order = dbContext.Add(order).Entity;
            await dbContext.SaveChangesAsync();

            var subscriptionOrder = new SubscriptionOrder()
            {
                OrderId = order.Id,
                Plan = planExtra.Plan,
                PlanExtraId = planExtra.Id
            };

            dbContext.Add(subscriptionOrder);

            if (dto.CouponId.HasValue)
            {
                order.Amount =
                    await couponService.ApplyCoupon(order.Amount, order.Id, order.UserId, dto.CouponId.Value);
                dbContext.Orders.Update(order);
            }

            await dbContext.SaveChangesAsync();

            if (order.Amount == 0)
            {
                await this.AcceptPaymentAsync(order.Id);
                paymentRequired = false;
                return;
            }

            await (dto.Provider switch
            {
                EnumPaymentProviders.Click => serviceProvider.GetRequiredService<ClickService>()
                    .CreateTransaction(order),
                EnumPaymentProviders.Payme => serviceProvider.GetRequiredService<PaymeService>()
                    .CreateInternalTransaction(order),
                EnumPaymentProviders.Iap => Task.CompletedTask,
                _ => throw new ProviderNotFoundException()
            });

            await dbContext.SaveChangesAsync();
        });

        if (!paymentRequired)
            return new CreateSubscriptionOrderResponseDto()
            {
                PaymentRequired = false,
                PaymentLink = "payment not required"
            };

        return new CreateSubscriptionOrderResponseDto()
        {
            PaymentRequired = true,
            PaymentLink = await this.MakePaymentLink(order.UserId, order.Id)
        };
    }

    public async Task<Wrapper> GetOrders(DataQueryRequest q, long? userId = null)
    {
        var query = dbContext.Orders.AsQueryable();

        if (userId is not null)
            query = query
                .Where(x => x.UserId == userId && x.Status == EnumOrderStatus.Pending);

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
                    ?? throw new OrderNotFoundException();

        await dbContext.Transactional(async () =>
        {
            order.Status = EnumOrderStatus.Cancelled;
            dbContext.Orders.Update(order);

            await (order.Provider switch
            {
                EnumPaymentProviders.Click => dbContext.ClickTransactions
                    .Where(x => x.OrderId == order.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(o => o.State, EnumClickTransactionState.Cancelled)),
                EnumPaymentProviders.Payme => dbContext.PaymeTransactions
                    .Where(x => x.OrderId == order.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(o => o.Status, EnumPaymeTransactionStatus.PaidCancelled)),
                EnumPaymentProviders.Iap => Task.CompletedTask,
                _ => throw new InvalidOperationException()
            });

            await dbContext.SaveChangesAsync();
        });
    }

    public async Task<bool> AcceptPaymentAsync(long orderId)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(x => x.Id == orderId);

        if (order is null) return false;

        var result = await (order.Type switch
        {
            EnumOrderType.Subscription => AcceptSubscriptionPaymentAsync(order),
            _ => throw new OrderTypeNotFoundException()
        });

        order.Status = result ? EnumOrderStatus.Confirmed : EnumOrderStatus.Cancelled;
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
                    ?? throw new OrderNotFoundException();

        return await (order.Provider switch
        {
            EnumPaymentProviders.Click => serviceProvider.GetRequiredService<ClickService>()
                .MakeClickPaymentLink(order.Id, order.Amount),
            EnumPaymentProviders.Payme => serviceProvider.GetRequiredService<PaymeService>()
                .MakeClickPaymentLink(order.Id, order.Amount),
            EnumPaymentProviders.Iap => Task.FromResult("3rd party payment"),
            _ => throw new ProviderNotFoundException()
        });
    }

    public async Task<Wrapper> GetPlanExtras(EnumSPlans plan, DataQueryRequest q)
    {
        var ordersStat = dbContext.SubscriptionOrders
            .GroupBy(x => x.PlanExtraId)
            .Select(x => new { ExtraId = x.Key, Count = x.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        var popularExtraId = ordersStat?.ExtraId ?? -1;

        return await dbContext
            .PlanExtras
            .Where(x => x.Plan == plan && x.IsActive)
            .Select(x => new GetPlanExtras
            {
                Id = x.Id, Duration = x.DurationInMonths, IsActive = x.IsActive,
                Plan = x.Plan,
                IsPopular = x.Id == popularExtraId,
                Fee = x.Fee,
                CreatedAt = x.CreatedAt
            })
            .GetByDataQueryAsync(q);
    }
}