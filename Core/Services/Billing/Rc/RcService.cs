using System.Text;
using BRB.Core.Common.Extensions;
using Core.Services.Billing.Rc.Exceptions;
using Core.Brokers.DbContext;
using Core.Entities.Billing.Enum;
using Core.Services.Auth;
using Core.Services.Billing.Rc.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Services.Billing.Rc;

/// <summary>
/// RevenueCat webhook. Hodisa turiga qarab:
/// <list type="bullet">
/// <item>INITIAL_PURCHASE / NON_RENEWING_PURCHASE (yoki turi yo'q — eski payload) — orderni qabul qiladi.</item>
/// <item>RENEWAL / UNCANCELLATION / PRODUCT_CHANGE — orderni (kerak bo'lsa) qabul qiladi va tugash sanasini RevenueCat'dagi bilan sinxronlaydi.</item>
/// <item>EXPIRATION — to'langan obunani o'chiradi.</item>
/// <item>Qolganlari (CANCELLATION, BILLING_ISSUE, TEST, ...) — hech narsa qilmaydi: user tugash sanasigacha premium.</item>
/// </list>
/// Hammasi idempotent — RevenueCat qayta yuborsa obuna ikki marta uzaytirilmaydi.
/// </summary>
public class RcService(
    AppDbContext dbContext,
    OrderService orderService,
    AuthService authService,
    IOptions<RcConfig> options,
    ILogger<RcService> logger)
{
    public async Task HandleRequest(RcRequest request)
    {
        var type = request.Event.Type?.Trim().ToUpperInvariant();

        var action = type switch
        {
            null or "" or "INITIAL_PURCHASE" or "NON_RENEWING_PURCHASE" => RcAction.Accept,
            "RENEWAL" or "UNCANCELLATION" or "PRODUCT_CHANGE" => RcAction.Sync,
            "EXPIRATION" => RcAction.Expire,
            _ => RcAction.Ignore
        };

        if (action == RcAction.Ignore)
        {
            logger.LogInformation("RevenueCat event {Type} ignored (event id {EventId})", type, request.Event.Id);
            return;
        }

        if (request.Event.SubscriberAttributes?.OrderId is null)
            throw new RcOrderIdRequiredException();

        var orderId = long.Parse(request.Event.SubscriberAttributes.OrderId.Value);
        var expiresAt = request.Event.ExpirationAtMs is { } ms
            ? DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime
            : (DateTime?)null;

        switch (action)
        {
            case RcAction.Accept:
            case RcAction.Sync:
                await orderService.AcceptPaymentAsync(orderId);
                if (expiresAt.HasValue)
                    await SyncExpiration(orderId, expiresAt.Value);
                break;
            case RcAction.Expire:
                await Expire(orderId);
                break;
        }
    }

    /// <summary>To'langan obunaning tugash sanasini RevenueCat'dagi bilan tenglashtiradi.</summary>
    private async Task SyncExpiration(long orderId, DateTime expiresAt)
    {
        // Qabul qilinmagan (bekor qilingan) order obunani faollashtirmasligi kerak.
        var userId = await dbContext.Orders
            .Where(x => x.Id == orderId && x.Status == EnumOrderStatus.Confirmed)
            .Select(x => (long?)x.UserId)
            .FirstOrDefaultAsync();
        if (userId is null) return;

        var subscription = await dbContext.Subscriptions.FirstOrDefaultAsync(x => x.UserId == userId);
        if (subscription is null) return;

        var wasActive = subscription.IsActive;

        subscription.EndsAt = expiresAt.AddDays(subscription.BonusDays);
        subscription.IsActive = true;
        subscription.Source = EnumSubscriptionSource.Payment;

        await dbContext.SaveChangesAsync();

        if (!wasActive)
            await authService.KillAllUserSessions(userId.Value);
    }

    private async Task Expire(long orderId)
    {
        var userId = await GetOrderUserId(orderId);
        if (userId is null) return;

        var now = DateTime.Now;

        var subscription = await dbContext.Subscriptions
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);

        // Faqat store boshqaradigan (Payment) obuna tugaydi; coin/referral premium o'z muddatida job bilan.
        if (subscription is null || subscription.Source != EnumSubscriptionSource.Payment)
            return;

        if (subscription.BonusDays > 0)
        {
            // To'lov tugadi, lekin referral/coin bonus kunlari qolgan — bonus sifatida davom etadi.
            subscription.Source = EnumSubscriptionSource.Referral;
            subscription.StartsAt = now;
            subscription.EndsAt = now.AddDays(subscription.BonusDays);
            subscription.BonusDays = 0;
        }
        else
        {
            subscription.IsActive = false;
            subscription.EndsAt = now;
        }

        await dbContext.SaveChangesAsync();
        await authService.KillAllUserSessions(userId.Value);
    }

    private Task<long?> GetOrderUserId(long orderId) =>
        dbContext.Orders
            .Where(x => x.Id == orderId)
            .Select(x => (long?)x.UserId)
            .FirstOrDefaultAsync();

    public void ValidateAuthentication(string? authorization)
    {
        if (authorization.IsNullOrEmpty())
            throw new RcInvalidAuthException();

        var parts = authorization!.Split();

        if (parts.Length != 2) throw new RcInvalidAuthException();

        if (!parts[0].Equals("Basic", StringComparison.InvariantCultureIgnoreCase)) throw new RcInvalidAuthException();

        var equals = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Value.Login}:{options.Value.Password}"))
            .Equals(parts[1], StringComparison.InvariantCultureIgnoreCase);

        if (!equals) throw new RcInvalidAuthException();
    }

    private enum RcAction
    {
        Accept,
        Sync,
        Expire,
        Ignore
    }
}
