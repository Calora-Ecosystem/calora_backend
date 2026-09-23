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
using Core.Services.Coins;
using Core.Services.Billing.Click;
using Core.Services.Billing.Contracts;
using Core.Services.Billing.Payme;
using Core.Helpers;
using Core.Services.Crm;
using Core.Services.Crm.Enum;
using Core.Services.User.Exceptions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResultWrapper.Library;

namespace Core.Services.Billing;

[Injectable]
public class OrderService(
    AppDbContext dbContext,
    IServiceProvider serviceProvider,
    AuthService authService,
    CouponService couponService,
    ReferralDiscountService referralDiscountService)
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

        // Referral chegirmasi faqat backend narxni belgilaydigan provayderlarda (Click/Payme).
        // IAP narxini App Store / Google Play belgilaydi.
        var referralDiscount = dto.Provider == EnumPaymentProviders.Iap
            ? 0
            : ReferralDiscountService.Calculate(planExtra.Fee,
                await referralDiscountService.GetAvailablePercent(userId));

        Order order = null!;
        bool paymentRequired = true;

        await dbContext.Transactional(async () =>
        {
            order = new Order()
            {
                Amount = planExtra.Fee - referralDiscount,
                ReferralDiscount = referralDiscount,
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

        // Idempotent: provayder (RevenueCat, Click, Payme) bir xil orderni qayta yuborsa,
        // obuna ikkinchi marta uzaytirilmaydi.
        if (order.Status == EnumOrderStatus.Confirmed) return true;

        var result = await (order.Type switch
        {
            EnumOrderType.Subscription => AcceptSubscriptionPaymentAsync(order),
            _ => throw new OrderTypeNotFoundException()
        });

        order.Status = result ? EnumOrderStatus.Confirmed : EnumOrderStatus.Cancelled;
        await dbContext.SaveChangesAsync();

        if (result && order.ReferralDiscount > 0)
            await referralDiscountService.MarkUsed(order.UserId, order.Id);

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

        // subscriptions.user_id unique — eski (muddati o'tgan yoki coin evaziga olingan) qatorni yangilaymiz.
        var subscription = await dbContext.Subscriptions.FirstOrDefaultAsync(x => x.UserId == order.UserId);

        if (subscription is null)
        {
            subscription = new Subscription { UserId = order.UserId };
            dbContext.Add(subscription);
        }

        // Faol premium (masalan coin/referral evaziga olingan) qolgan kunlari yo'qolmaydi.
        var hasActiveGrant = subscription is { IsActive: true, Id: > 0 } && subscription.EndsAt > now;

        if (order.Provider == EnumPaymentProviders.Iap)
        {
            // IAP: EndsAt RevenueCat'dan keladi (RcService.SyncExpiration), qolgan kunlar bonus bo'ladi.
            subscription.BonusDays = hasActiveGrant && subscription.Source != EnumSubscriptionSource.Payment
                ? (int)Math.Ceiling((subscription.EndsAt - now).TotalDays)
                : 0;
            subscription.StartsAt = now;
            subscription.EndsAt = now.AddMonths(orderExtra.PlanExtra.DurationInMonths)
                .AddDays(subscription.BonusDays);
        }
        else
        {
            var startsFrom = hasActiveGrant ? subscription.EndsAt : now;

            if (!hasActiveGrant)
                subscription.StartsAt = now;

            subscription.BonusDays = 0;
            subscription.EndsAt = startsFrom.AddMonths(orderExtra.PlanExtra.DurationInMonths);
        }

        subscription.SubscriptionPlan = orderExtra.Plan;
        subscription.IsActive = true;
        subscription.Source = EnumSubscriptionSource.Payment;

        await dbContext.SaveChangesAsync();
        
        BackgroundJob.Enqueue<LeadService>(service => service.HandleEventAsync(new Core.Services.Crm.Contracts.HandleLeadEventDto(subscription.UserId, EnumLeadEvent.Purchased)));

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

    public async Task<Wrapper> GetPlanExtras(EnumSPlans plan, DataQueryRequest q, long? userId = null)
    {
        var percent = userId.HasValue ? await referralDiscountService.GetAvailablePercent(userId.Value) : 0;

        return await dbContext
            .PlanExtras
            .Where(x => x.Plan == plan && x.IsActive)
            .Select(x => new GetPlanExtras
            {
                Id = x.Id, Duration = x.DurationInMonths, IsActive = x.IsActive,
                Plan = x.Plan,
                IsPopular = x.IsPopular,
                Fee = x.Fee / 100d,
                OriginalFee = x.OriginalFee / 100d,
                ReferralDiscountPercent = percent,
                DiscountedFee = (x.Fee - x.Fee * percent / 100) / 100d,
                CreatedAt = x.CreatedAt
            })
            .GetByDataQueryAsync(q);
    }

    /// <summary>
    /// TEMPORARY (staging test helper): disables a user's premium by phone
    /// number so they can re-purchase a subscription. Deactivates all of the
    /// user's active subscriptions. Remove before production.
    /// </summary>
    public async Task<int> DisablePremiumByPhone(string phone)
    {
        var validPhone = FormatHelper.MakeValidPhone(phone.Trim());

        var userIds = await dbContext.Users
            .Where(x => x.Phone == validPhone)
            .Select(x => x.Id)
            .ToListAsync();

        if (userIds.Count == 0)
            throw new UserNotFoundException();

        return await dbContext.Subscriptions
            .Where(x => userIds.Contains(x.UserId) && x.IsActive)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.EndsAt, DateTime.Now));
    }
}