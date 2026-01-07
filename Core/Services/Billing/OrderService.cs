using BRB.Core.Common.Exceptions;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Services.Billing.Click;
using Core.Services.Billing.Contracts;
using Core.Services.Billing.Payme;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Services.Billing;

[Injectable]
public class OrderService(AppDbContext dbContext, IServiceProvider serviceProvider)
{
    public async Task<string> CreateSubscriptionOrder(long userId, CreateSubscriptionOrderDto dto)
    {
        await dbContext.Users.ExistsOrThrowsNotFoundException(userId);
        var planExtra = await dbContext.PlanExtras.FirstOrDefaultAsync(x => x.Plan == dto.Plan && x.IsActive) ??
                        throw new NotFoundException("Active plan not found");

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
                Plan = planExtra.Plan
            };

            dbContext.Add(subscriptionOrder);

            await (dto.Provider switch
            {
                EnumPaymentProviders.Click => serviceProvider.GetRequiredService<ClickService>()
                    .CreateTransaction(order),
                EnumPaymentProviders.Payme => serviceProvider.GetRequiredService<PaymeService>()
                    .CreateTransaction(order),
                _ => throw new Exception("Provider not found")
            });

            await dbContext.SaveChangesAsync();
        });

        return await this.MakePaymentLink(order.UserId, order.Id);
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
            .FirstOrDefaultAsync(x => x.OrderId == order.Id);

        if (orderExtra is null) return false;

        var now = DateTime.Now;

        var planExtra = await dbContext.PlanExtras.FirstOrDefaultAsync(x => x.Plan == orderExtra.Plan && x.IsActive);

        if (planExtra is null) return false;

        var subscription = new Subscription()
        {
            SubscriptionPlan = orderExtra.Plan,
            UserId = order.UserId,
            StartsAt = now,
            EndsAt = now.Add(planExtra.Duration),
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
            EnumPaymentProviders.Payme => Task.FromResult("change-me"),
            _ => throw new Exception("Provider not found")
        });
    }
}