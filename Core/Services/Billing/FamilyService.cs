using BRB.Core.EF.Attributes;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Entities.Billing.Enum;
using Core.Enums;
using Core.Helpers;
using Core.Services.Billing.Contracts;
using Core.Services.Billing.Exceptions;
using Core.Services.Notification;
using Core.Services.Notification.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Services.Billing;

/// <summary>
/// Oilaviy tarif (2 kishi).
/// <list type="number">
/// <item>User oilaviy paketni (<see cref="PlanExtra.IsFamily"/>) Click/Payme orqali sotib oladi —
/// o'zi oddiy to'lov kabi premium bo'ladi (<see cref="OrderService"/>).</item>
/// <item>To'lov tasdiqlanganda shu order uchun bitta <see cref="FamilyCode"/> yaratiladi
/// (<see cref="IssueForOrder"/>) va egasiga push yuboriladi; ilova kodni nusxalash/ulashish uchun ko'rsatadi.</item>
/// <item>Ikkinchi odam kodni kiritadi (<see cref="Redeem"/>) — unga paket muddaticha premium
/// (source=Family) beriladi, egasiga push boradi. Kod bir marta ishlatiladi.</item>
/// </list>
/// </summary>
[Injectable]
public class FamilyService(
    AppDbContext dbContext,
    SubscriptionService subscriptionService,
    NotificationService notificationService,
    ILogger<FamilyService> logger)
{
    private const string CodePrefix = "FAMILY-";
    private const int CodeLength = 6;

    /// <summary>Kod shu kun ichida faollashtirilishi kerak — to'langan oy ichida ishlatilsin.</summary>
    public const int RedeemWindowDays = 30;

    /// <summary>
    /// Oilaviy order to'langanda ikkinchi odam uchun kod qo'shadi (saqlamaydi — chaqiruvchi
    /// obuna bilan birga bitta <c>SaveChanges</c>da saqlaydi). Shu order uchun kod bo'lsa — null.
    /// </summary>
    public async Task<FamilyCode?> IssueForOrder(long ownerId, long orderId, int months)
    {
        if (await dbContext.FamilyCodes.AnyAsync(x => x.OrderId == orderId))
            return null;

        string code;
        do code = CodeGenerator.Generate(CodePrefix, CodeLength);
        while (await dbContext.FamilyCodes.AnyAsync(x => x.Code == code));

        var familyCode = new FamilyCode
        {
            OwnerId = ownerId,
            OrderId = orderId,
            Code = code,
            Months = Math.Max(1, months),
            ExpireAt = DateTime.Now.AddDays(RedeemWindowDays)
        };

        dbContext.FamilyCodes.Add(familyCode);
        return familyCode;
    }

    /// <summary>Oilaviy tarif egasining kodlari, eng yangisi birinchi.</summary>
    public async Task<List<FamilyCodeDto>> GetMy(long userId)
    {
        var now = DateTime.Now;

        return await dbContext.FamilyCodes
            .AsNoTracking()
            .Where(x => x.OwnerId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(20)
            .Select(x => new FamilyCodeDto
            {
                Code = x.Code,
                Months = x.Months,
                Status = x.RedeemedById != null
                    ? EnumFamilyCodeStatus.Redeemed
                    : x.ExpireAt < now
                        ? EnumFamilyCodeStatus.Expired
                        : EnumFamilyCodeStatus.Active,
                CreatedAt = x.CreatedAt,
                ExpireAt = x.ExpireAt,
                RedeemedAt = x.RedeemedAt,
                RedeemedBy = x.RedeemedBy != null ? x.RedeemedBy.Name : null
            })
            .ToListAsync();
    }

    /// <summary>
    /// Ikkinchi odam kodni kiritadi: kod band qilinadi va unga paket muddaticha premium beriladi.
    /// O'z kodini, ishlatilgan yoki muddati o'tgan kodni ishlatib bo'lmaydi.
    /// </summary>
    public async Task<RedeemFamilyCodeResultDto> Redeem(long userId, RedeemFamilyCodeDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var now = DateTime.Now;

        var familyCode = await dbContext.FamilyCodes
                             .AsNoTracking()
                             .Where(x => x.Code == code)
                             .Select(x => new
                             {
                                 x.Id, x.OwnerId, x.Months, x.ExpireAt, x.RedeemedById,
                                 OwnerName = x.Owner.Name
                             })
                             .FirstOrDefaultAsync()
                         ?? throw new FamilyCodeNotFoundException();

        if (familyCode.OwnerId == userId)
            throw new FamilyCodeSelfException();

        if (familyCode.RedeemedById != null)
            throw new FamilyCodeUsedException();

        if (familyCode.ExpireAt < now)
            throw new FamilyCodeExpiredException();

        var days = (int)Math.Round((now.AddMonths(familyCode.Months) - now).TotalDays);

        await dbContext.Transactional(async () =>
        {
            // Atomik — bitta kodni ikki kishi (yoki parallel so'rov) ishlata olmaydi.
            var claimed = await dbContext.FamilyCodes
                .Where(x => x.Id == familyCode.Id && x.RedeemedById == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.RedeemedById, userId)
                    .SetProperty(x => x.RedeemedAt, now)
                    .SetProperty(x => x.UpdatedAt, now));

            if (claimed == 0)
                throw new FamilyCodeUsedException();

            await subscriptionService.GrantPremiumDays(userId, days, EnumSubscriptionSource.Family);
        });

        var endsAt = await dbContext.Subscriptions
            .Where(x => x.UserId == userId)
            .Select(x => x.EndsAt)
            .FirstAsync();

        var redeemerName = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync();

        await Notify(familyCode.OwnerId, FamilyPush.Redeemed, redeemerName);

        return new RedeemFamilyCodeResultDto
        {
            OwnerName = familyCode.OwnerName,
            Months = familyCode.Months,
            EndsAt = endsAt
        };
    }

    /// <summary>Egasiga yangi kod haqida push — ilovada oyna yopilib qolsa ham kod yo'qolmasin.</summary>
    public Task NotifyIssued(FamilyCode familyCode) =>
        Notify(familyCode.OwnerId, FamilyPush.Issued, familyCode.Code);

    private enum FamilyPush
    {
        Issued,
        Redeemed
    }

    private async Task Notify(long ownerId, FamilyPush push, string? value)
    {
        try
        {
            var language = await dbContext.UserExtras
                .Where(x => x.UserId == ownerId)
                .Select(x => (EnumLanguage?)x.Language)
                .FirstOrDefaultAsync() ?? EnumLanguage.Uzbek;

            var name = string.IsNullOrWhiteSpace(value) ? null : value;

            var (title, description) = (push, language) switch
            {
                (FamilyPush.Issued, EnumLanguage.Russian) => ("👨‍👩 Семейный тариф активен!", $"Код для близкого: {value}. Отправьте его — он получит Premium на месяц."),
                (FamilyPush.Issued, EnumLanguage.English) => ("👨‍👩 Family plan is on!", $"Code for your partner: {value}. Send it — they get a month of Premium."),
                (FamilyPush.Issued, EnumLanguage.Cyrillic) => ("👨‍👩 Оилавий тариф фаол!", $"Яқинингиз учун код: {value}. Юборинг — у бир ойлик Premium олади."),
                (FamilyPush.Issued, _) => ("👨‍👩 Oilaviy tarif faol!", $"Yaqiningiz uchun kod: {value}. Yuboring — u bir oylik Premium oladi."),

                (FamilyPush.Redeemed, EnumLanguage.Russian) => ("🎉 Код активирован!", $"{name ?? "Ваш близкий"} теперь тоже с Premium."),
                (FamilyPush.Redeemed, EnumLanguage.English) => ("🎉 Code activated!", $"{name ?? "Your partner"} is on Premium too now."),
                (FamilyPush.Redeemed, EnumLanguage.Cyrillic) => ("🎉 Код фаоллашди!", $"{name ?? "Яқинингиз"} ҳам энди Premium'да."),
                (FamilyPush.Redeemed, _) => ("🎉 Kod faollashdi!", $"{name ?? "Yaqiningiz"} ham endi Premium'da."),

                _ => ("", "")
            };

            await notificationService.CreateOrUpdatePushNotification(new PushNotificationDto
            {
                UserId = ownerId,
                Title = title,
                Description = description,
                Meta = new Dictionary<string, string> { ["type"] = "family", ["event"] = push.ToString() }
            });
        }
        catch (Exception ex)
        {
            // Bildirishnoma yuborilmasa ham asosiy amal bajarilgan.
            logger.LogWarning(ex, "Failed to enqueue family push for user {UserId}", ownerId);
        }
    }
}
