using BRB.Core.Common.Models;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing.Enum;
using Core.Entities.Coins;
using Core.Entities.Coins.Enum;
using Core.Enums;
using Core.Helpers;
using Core.Services.Billing;
using Core.Services.Coins.Contracts;
using Core.Services.Coins.Exceptions;
using Core.Services.Notification;
using Core.Services.Notification.Contracts;
using Core.Services.User.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResultWrapper.Library;

namespace Core.Services.Coins;

/// <summary>
/// "Do'stni taklif qilish".
/// <list type="number">
/// <item>Har bir userning taklif kodi bor (<c>CALORA-XXXX</c>).</item>
/// <item>User do'stining kodini Invite friends sahifasida tasdiqlaydi (<see cref="Apply"/>) — "men shu userdan kirdim".</item>
/// <item>Do'st onboarding'ni tugatgach (profil yaratilgach) "faol" bo'ladi (<see cref="TryQualify"/>).</item>
/// <item>Har <see cref="CoinConfig.ReferralPremiumFriends"/> ta faol do'st uchun taklif qiluvchiga
/// <see cref="CoinConfig.ReferralPremiumDays"/> kun premium beriladi.</item>
/// <item>Taklif qilingan user birinchi premium xaridida <see cref="CoinConfig.ReferredDiscountPercent"/>% chegirma oladi
/// (<see cref="ReferralDiscountService"/>).</item>
/// </list>
/// </summary>
[Injectable]
public class ReferralService(
    AppDbContext dbContext,
    CoinService coinService,
    SubscriptionService subscriptionService,
    ReferralDiscountService referralDiscountService,
    NotificationService notificationService,
    IOptions<CoinConfig> options,
    ILogger<ReferralService> logger)
{
    private const string CodePrefix = "CALORA-";
    private const string ReferrerTxTitle = "coin_tx_referral";
    private const string ReferredTxTitle = "coin_tx_referral_welcome";
    private CoinConfig Config => options.Value;

    public async Task<ReferralInfoDto> GetMy(long userId)
    {
        var code = await GetOrCreateCode(userId);

        var invited = await dbContext.Referrals.CountAsync(x => x.ReferrerId == userId);
        var active = await dbContext.Referrals.CountAsync(x => x.ReferrerId == userId && x.QualifiedAt != null);
        var premiumsEarned = await dbContext.ReferralPremiumGrants.CountAsync(x => x.ReferrerId == userId);

        var referredBy = await dbContext.Referrals
            .Where(x => x.ReferredUserId == userId)
            .Select(x => x.Referrer.Name)
            .FirstOrDefaultAsync();

        var createdAt = await dbContext.Users.Where(x => x.Id == userId).Select(x => x.CreatedAt).FirstAsync();

        var goal = Math.Max(1, Config.ReferralPremiumFriends);
        var progressFriends = active % goal;
        var discountPercent = await referralDiscountService.GetAvailablePercent(userId);

        return new ReferralInfoDto
        {
            Code = code,
            Invited = invited,
            Active = active,
            FriendsGoal = goal,
            PremiumDays = Config.ReferralPremiumDays,
            ProgressFriends = progressFriends,
            FriendsLeft = goal - progressFriends,
            Progress = progressFriends / (double)goal,
            PremiumsEarned = premiumsEarned,
            IsReferred = referredBy is not null,
            ReferredBy = referredBy,
            CanApplyCode = referredBy is null && IsWithinApplyWindow(createdAt),
            DiscountPercent = Config.ReferredDiscountPercent,
            HasDiscount = discountPercent > 0
        };
    }

    /// <summary>User taklif qilgan do'stlar va ularning holati (ro'yxatdan o'tdi / ilovaga kirdi).</summary>
    public async Task<Wrapper> GetInvited(long userId, DataQueryRequest q)
    {
        return await dbContext.Referrals
            .AsNoTracking()
            .Where(x => x.ReferrerId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReferredFriendDto
            {
                UserId = x.ReferredUserId,
                Name = x.ReferredUser.Name,
                Photo = dbContext.UserExtras
                    .Where(e => e.UserId == x.ReferredUserId)
                    .Select(e => e.Photo)
                    .FirstOrDefault(),
                Status = x.QualifiedAt != null ? EnumReferralStatus.Active : EnumReferralStatus.Joined,
                JoinedAt = x.CreatedAt,
                ActivatedAt = x.QualifiedAt
            })
            .GetByDataQueryAsync(q);
    }

    /// <summary>
    /// User do'stining kodini tasdiqlaydi ("men shu userdan kirdim"). Bir marta, o'z kodini emas;
    /// <see cref="CoinConfig.ReferralApplyWindowDays"/> &gt; 0 bo'lsa faqat shu kun ichida.
    /// </summary>
    public async Task<ApplyReferralResultDto> Apply(long userId, ApplyReferralCodeDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();

        if (await dbContext.Referrals.AnyAsync(x => x.ReferredUserId == userId))
            throw new ReferralAlreadyAppliedException();

        var user = await dbContext.Users
                       .AsNoTracking()
                       .Where(x => x.Id == userId)
                       .Select(x => new { x.Id, x.CreatedAt })
                       .FirstOrDefaultAsync()
                   ?? throw new UserNotFoundException();

        if (!IsWithinApplyWindow(user.CreatedAt))
            throw new ReferralWindowExpiredException();

        var referrer = await dbContext.Users
                           .AsNoTracking()
                           .Where(x => x.ReferralCode == code)
                           .Select(x => new { x.Id, x.Name })
                           .FirstOrDefaultAsync()
                       ?? throw new ReferralCodeNotFoundException();

        // O'z kodi yoki o'zaro (A→B, B→A) taklif — farmingdan himoya.
        if (referrer.Id == userId ||
            await dbContext.Referrals.AnyAsync(x => x.ReferrerId == userId && x.ReferredUserId == referrer.Id))
            throw new ReferralSelfException();

        var referral = new Referral
        {
            ReferrerId = referrer.Id,
            ReferredUserId = userId,
            ReferrerReward = Config.ReferralReward,
            ReferredReward = Config.ReferredReward
        };

        try
        {
            await dbContext.Transactional(async () =>
            {
                dbContext.Referrals.Add(referral);
                await dbContext.SaveChangesAsync();

                await coinService.Credit(userId, referral.ReferredReward, EnumCoinTxType.Referral,
                    ReferredTxTitle, referral.Id);
            });
        }
        catch (DbUpdateException)
        {
            // ReferredUserId unique — parallel so'rov allaqachon yozib bo'lgan.
            throw new ReferralAlreadyAppliedException();
        }

        await Notify(referrer.Id, ReferralPush.Joined);

        // Kod onboarding'dan keyin (profil tayyor) tasdiqlangan bo'lsa — darhol faol.
        await TryQualify(userId);

        return new ApplyReferralResultDto
        {
            ReferrerName = referrer.Name,
            DiscountPercent = Config.ReferredDiscountPercent,
            Reward = referral.ReferredReward
        };
    }

    /// <summary>
    /// Taklif qilingan user ilovaga to'liq kirgach (profil yaratilgach) chaqiriladi.
    /// Referralni faol qiladi va taklif qiluvchiga har N ta faol do'st uchun premium beradi.
    /// Xatolar yutiladi — profil saqlashni buzmasligi kerak.
    /// </summary>
    public async Task TryQualify(long referredUserId)
    {
        try
        {
            if (!await dbContext.UserExtras.AnyAsync(x => x.UserId == referredUserId))
                return;

            var now = DateTime.Now;

            // Atomik — parallel so'rovlarda bir do'st ikki marta hisoblanmaydi.
            var qualified = await dbContext.Referrals
                .Where(x => x.ReferredUserId == referredUserId && x.QualifiedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.QualifiedAt, now)
                    .SetProperty(x => x.UpdatedAt, now));

            if (qualified == 0)
                return;

            var referral = await dbContext.Referrals
                .AsNoTracking()
                .Where(x => x.ReferredUserId == referredUserId)
                .Select(x => new { x.Id, x.ReferrerId, x.ReferrerReward })
                .FirstAsync();

            await coinService.Credit(referral.ReferrerId, referral.ReferrerReward, EnumCoinTxType.Referral,
                ReferrerTxTitle, referral.Id);

            var granted = await GrantMilestones(referral.ReferrerId);

            await Notify(referral.ReferrerId, granted > 0 ? ReferralPush.PremiumGranted : ReferralPush.Activated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Referral qualification failed for user {UserId}", referredUserId);
        }
    }

    /// <summary>
    /// Faol do'stlar soni bo'yicha hali berilmagan premium bosqichlarini beradi.
    /// (ReferrerId, Milestone) unique index bir bosqichni ikki marta berishdan himoya qiladi.
    /// </summary>
    private async Task<int> GrantMilestones(long referrerId)
    {
        var goal = Math.Max(1, Config.ReferralPremiumFriends);
        var days = Config.ReferralPremiumDays;
        if (days <= 0) return 0;

        var active = await dbContext.Referrals.CountAsync(x => x.ReferrerId == referrerId && x.QualifiedAt != null);
        var reached = active / goal;

        var existing = await dbContext.ReferralPremiumGrants
            .Where(x => x.ReferrerId == referrerId)
            .Select(x => x.Milestone)
            .ToListAsync();

        var granted = 0;

        for (var milestone = 1; milestone <= reached; milestone++)
        {
            if (existing.Contains(milestone))
                continue;

            try
            {
                await dbContext.Transactional(async () =>
                {
                    dbContext.ReferralPremiumGrants.Add(new ReferralPremiumGrant
                    {
                        ReferrerId = referrerId,
                        Milestone = milestone,
                        Days = days
                    });
                    await dbContext.SaveChangesAsync();

                    await subscriptionService.GrantPremiumDays(referrerId, days, EnumSubscriptionSource.Referral);
                });

                granted++;
            }
            catch (DbUpdateException)
            {
                // Parallel so'rov bu bosqichni allaqachon bergan.
                dbContext.ChangeTracker.Clear();
            }
        }

        return granted;
    }

    private bool IsWithinApplyWindow(DateTime userCreatedAt) =>
        Config.ReferralApplyWindowDays <= 0 ||
        userCreatedAt >= DateTime.Now.AddDays(-Config.ReferralApplyWindowDays);

    private async Task<string> GetOrCreateCode(long userId)
    {
        var existing = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => x.ReferralCode)
            .FirstOrDefaultAsync();

        if (existing is not null)
            return existing;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = CodeGenerator.Generate(CodePrefix, 4);

            if (await dbContext.Users.IgnoreQueryFilters().AnyAsync(x => x.ReferralCode == code))
                continue;

            try
            {
                var updated = await dbContext.Users
                    .Where(x => x.Id == userId && x.ReferralCode == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReferralCode, code));

                if (updated > 0)
                    return code;

                // Parallel so'rov allaqachon kod yaratgan.
                return await dbContext.Users.Where(x => x.Id == userId).Select(x => x.ReferralCode!).FirstAsync();
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
            {
                // Unique to'qnashuv — boshqa kod bilan qayta urinamiz.
            }
        }

        throw new InvalidOperationException("Could not generate a unique referral code");
    }

    private enum ReferralPush
    {
        Joined,
        Activated,
        PremiumGranted
    }

    private async Task Notify(long referrerId, ReferralPush push)
    {
        try
        {
            var language = await dbContext.UserExtras
                .Where(x => x.UserId == referrerId)
                .Select(x => (EnumLanguage?)x.Language)
                .FirstOrDefaultAsync() ?? EnumLanguage.Uzbek;

            var active = await dbContext.Referrals.CountAsync(x => x.ReferrerId == referrerId && x.QualifiedAt != null);
            var goal = Math.Max(1, Config.ReferralPremiumFriends);
            var progress = active % goal;
            var left = goal - progress;
            var days = Config.ReferralPremiumDays;

            var (title, description) = (push, language) switch
            {
                (ReferralPush.Joined, EnumLanguage.Russian) => ("🎉 Друг зарегистрировался по вашему коду!", "Как только он начнёт пользоваться приложением, он засчитается."),
                (ReferralPush.Joined, EnumLanguage.English) => ("🎉 A friend signed up with your code!", "They'll count once they start using the app."),
                (ReferralPush.Joined, EnumLanguage.Cyrillic) => ("🎉 Дўстингиз кодингиз орқали рўйхатдан ўтди!", "Иловага тўлиқ киргач ҳисобга олинади."),
                (ReferralPush.Joined, _) => ("🎉 Do'stingiz kodingiz orqali ro'yxatdan o'tdi!", "Ilovaga to'liq kirgach hisobga olinadi."),

                (ReferralPush.Activated, EnumLanguage.Russian) => ("👏 Друг в приложении!", $"{progress}/{goal} — ещё {left} до Premium на {days} дней"),
                (ReferralPush.Activated, EnumLanguage.English) => ("👏 Your friend is in!", $"{progress}/{goal} — {left} more for {days} days of Premium"),
                (ReferralPush.Activated, EnumLanguage.Cyrillic) => ("👏 Дўстингиз иловага кирди!", $"{progress}/{goal} — {days} кунлик Premiumгача яна {left} та"),
                (ReferralPush.Activated, _) => ("👏 Do'stingiz ilovaga kirdi!", $"{progress}/{goal} — {days} kunlik Premiumgacha yana {left} ta"),

                (ReferralPush.PremiumGranted, EnumLanguage.Russian) => ("👑 Premium на " + days + " дней!", $"{goal} друзей присоединились — Premium активирован."),
                (ReferralPush.PremiumGranted, EnumLanguage.English) => ("👑 " + days + " days of Premium!", $"{goal} friends joined — Premium is on."),
                (ReferralPush.PremiumGranted, EnumLanguage.Cyrillic) => ("👑 " + days + " кунлик Premium!", $"{goal} та дўстингиз қўшилди — Premium фаоллашди."),
                (ReferralPush.PremiumGranted, _) => ("👑 " + days + " kunlik Premium!", $"{goal} ta do'stingiz qo'shildi — Premium faollashdi."),

                _ => ("", "")
            };

            await notificationService.CreateOrUpdatePushNotification(new PushNotificationDto
            {
                UserId = referrerId,
                Title = title,
                Description = description,
                Meta = new Dictionary<string, string> { ["type"] = "referral", ["event"] = push.ToString() }
            });
        }
        catch (Exception ex)
        {
            // Bildirishnoma yuborilmasa ham asosiy amal bajarilgan.
            logger.LogWarning(ex, "Failed to enqueue referral push for user {UserId}", referrerId);
        }
    }
}
