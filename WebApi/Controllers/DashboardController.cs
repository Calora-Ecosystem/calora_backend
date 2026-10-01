using BRB.Core.Common.Models;
using Core.Attributes;
using Core.Entities.Coins.Enum;
using Core.Enums;
using Core.Services.Billing;
using Core.Services.Billing.Contracts;
using Core.Services.Coins;
using Core.Services.Coins.Contracts;
using Core.Services.Dashboard;
using Core.Services.Dashboard.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

[ApiController]
[Route("dashboard")]
[RoleAuthorize(EnumRole.SuperAdmin)]
public class DashboardController(
    DashboardService dashboardService,
    SubscriptionService subscriptionService,
    CoinService coinService,
    CoinRuleService coinRuleService,
    MarketAdminService marketAdminService,
    ReferralAdminService referralAdminService)
    : AuthorizedController
{
    [HttpGet("summary")]
    [ProducesResponseType<WrapperGeneric<GetOverallSummaryDto>>(200)]
    public async Task<Wrapper> GetOverallSummary() =>
        (await dashboardService.GetOverallSummary(), 200);

    [HttpGet("sales/summary")]
    [ProducesResponseType<WrapperGeneric<Dictionary<int, double>>>(200)]
    public async Task<Wrapper> SalesSummary() =>
        (await dashboardService.GetSalesMonthlySummary(), 200);

    [HttpGet("users/statistics")]
    [ProducesResponseType<WrapperGeneric<GetUserStatisticsDto>>(200)]
    public async Task<Wrapper> GetUserStatistics() =>
        (await dashboardService.GetUserStatistics(), 200);

    [HttpGet("users/statistics/range")]
    [ProducesResponseType<WrapperGeneric<GetUserStatisticsRangeDto>>(200)]
    public async Task<Wrapper> GetUserStatisticsRange([FromQuery] DateTime from, [FromQuery] DateTime to) =>
        (await dashboardService.GetUserStatisticsRange(from, to), 200);

    [HttpGet("users/audience")]
    [ProducesResponseType<WrapperGeneric<GetAudienceAnalyticsDto>>(200)]
    public async Task<Wrapper> GetAudienceAnalytics() =>
        (await dashboardService.GetAudienceAnalytics(), 200);

    [HttpGet("users/{userId:long:min(1)}/detail")]
    [ProducesResponseType<WrapperGeneric<GetUserDetailDto>>(200)]
    public async Task<Wrapper> GetUserDetail(long userId) =>
        (await dashboardService.GetUserDetail(userId), 200);

    [HttpGet("orders/subscriptions")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetSubscriptionOrdersDto>>>(200)]
    public async Task<Wrapper> GetAllOrders([FromQuery] DataQueryRequest query) =>
        await dashboardService.GetSubscriptionOrders(query);

    #region Event log (general-purpose analytics — AI, to'lovlar, notification va h.k.)

    [HttpGet("events/sources")]
    [ProducesResponseType<WrapperGeneric<List<string>>>(200)]
    public async Task<Wrapper> GetEventLogSources() =>
        (await dashboardService.GetEventLogSources(), 200);

    [HttpGet("events/summary")]
    [ProducesResponseType<WrapperGeneric<GetEventLogSummaryDto>>(200)]
    public async Task<Wrapper> GetEventLogSummary(
        [FromQuery] string source, [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        (await dashboardService.GetEventLogSummary(source, from, to), 200);

    #endregion

    #region AI Analytics (food recognition usage, cost, tokens, premium adoption)

    [HttpGet("ai/statistics")]
    [ProducesResponseType<WrapperGeneric<GetAiStatisticsDto>>(200)]
    public async Task<Wrapper> GetAiStatistics(
        [FromQuery] int? days = 28, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null) =>
        (await dashboardService.GetAiStatistics(days, from, to), 200);

    #endregion

    #region Coins (reyting, g'oliblarni aniqlash)

    /// <summary>Davr bo'yicha coin statistikasi. Davr berilmasa — coin ishga tushgan kundan bugungacha.</summary>
    [HttpGet("coins/summary")]
    [ProducesResponseType<WrapperGeneric<AdminCoinSummaryDto>>(200)]
    public async Task<Wrapper> GetCoinSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        (await coinService.AdminSummary(from, to), 200);

    /// <summary>
    /// Coin reytingi: davrda (<c>from</c>/<c>to</c> — kunlar, ikkalasi ham kiradi) ishlab topilgan coinlar.
    /// <c>search</c> — ism, email, telefon yoki user id; <c>rank</c> umumiy reytingdagi o'rin bo'lib qoladi.
    /// </summary>
    [HttpGet("coins/ranking")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<AdminCoinRankingDto>>>(200)]
    public Task<Wrapper> GetCoinRanking([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? search, [FromQuery] DataQueryRequest q) =>
        coinService.AdminRanking(from, to, search, q);

    /// <summary>Userning hamyoni, davr reytingidagi o'rni va kunma-kun coinlari.</summary>
    [HttpGet("coins/users/{userId:long:min(1)}")]
    [ProducesResponseType<WrapperGeneric<AdminUserCoinsDto>>(200)]
    public async Task<Wrapper> GetUserCoins(long userId, [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        (await coinService.AdminUserCoins(userId, from, to), 200);

    /// <summary>Userning hamyon tarixi (<c>wallet/transactions</c> bilan bir xil).</summary>
    [HttpGet("coins/users/{userId:long:min(1)}/transactions")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<CoinTransactionDto>>>(200)]
    public Task<Wrapper> GetUserCoinTransactions(long userId, [FromQuery] DataQueryRequest q,
        [FromQuery] EnumCoinTxType? type) =>
        coinService.GetTransactions(userId, q, type);

    #endregion

    #region Coin qoidasi (har N qadam = 1 coin)

    /// <summary>Joriy, kelajakdagi va o'tgan qadam → coin qoidalari.</summary>
    [HttpGet("coins/rules")]
    [ProducesResponseType<WrapperGeneric<AdminCoinRulesDto>>(200)]
    public async Task<Wrapper> GetCoinRules() => (await coinRuleService.GetAdminRules(), 200);

    /// <summary>
    /// Yangi qoida: <c>effectiveFrom</c> (bugun yoki kelajak, berilmasa bugun) kunidan boshlab.
    /// O'tgan kunlar o'z qoidasi bilan qoladi. O'sha kunga qoida bo'lsa — yangilanadi.
    /// </summary>
    [HttpPost("coins/rules")]
    [ProducesResponseType<WrapperGeneric<AdminCoinRulesDto>>(200)]
    public async Task<Wrapper> SaveCoinRule([FromBody] SaveCoinRuleDto dto) =>
        (await coinRuleService.Save(dto, this.UserId), 200);

    /// <summary>Bugungi yoki kelajakdagi qoidani o'chirish (oldingi qoida davom etadi).</summary>
    [HttpDelete("coins/rules/{ruleId:long:min(1)}")]
    [ProducesResponseType<WrapperGeneric<AdminCoinRulesDto>>(200)]
    public async Task<Wrapper> DeleteCoinRule(long ruleId) => (await coinRuleService.Delete(ruleId), 200);

    /// <summary>Oxirgi <c>days</c> kun haqiqiy qadamlari bo'yicha joriy va taklif qilingan qoida natijasi.</summary>
    [HttpGet("coins/rules/preview")]
    [ProducesResponseType<WrapperGeneric<CoinRulePreviewDto>>(200)]
    public async Task<Wrapper> PreviewCoinRule([FromQuery] int stepsPerCoin, [FromQuery] int maxDailyCoins,
        [FromQuery] int days = 30) =>
        (await coinRuleService.Preview(stepsPerCoin, maxDailyCoins, days), 200);

    #endregion

    #region Coin do'koni (tariflar narxi)

    /// <summary>Barcha mahsulotlar (faolsizlari ham) va davrdagi sotuvlari.</summary>
    [HttpGet("market/items")]
    [ProducesResponseType<WrapperGeneric<List<AdminMarketItemDto>>>(200)]
    public async Task<Wrapper> GetMarketItems([FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        (await marketAdminService.GetItems(from, to), 200);

    /// <summary>Mahsulot yaratish (<c>id</c> yo'q) yoki yangilash: narx (coin), kunlar, faollik, mashhur.</summary>
    [HttpPost("market/items")]
    [ProducesResponseType<WrapperGeneric<MarketItemDto>>(200)]
    public async Task<Wrapper> SaveMarketItem([FromBody] CreateOrUpdateMarketItemDto dto) =>
        (await coinService.CreateOrUpdateMarketItem(dto), 200);

    /// <summary>Xarid qilinmagan mahsulotni o'chirish (xarid qilinganini faolsizlantiring).</summary>
    [HttpDelete("market/items/{marketItemId:long:min(1)}")]
    public async Task<Wrapper> DeleteMarketItem(long marketItemId)
    {
        await coinService.DeleteMarketItem(marketItemId);
        return 200;
    }

    [HttpGet("market/summary")]
    [ProducesResponseType<WrapperGeneric<AdminMarketSummaryDto>>(200)]
    public async Task<Wrapper> GetMarketSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        (await marketAdminService.GetSummary(from, to), 200);

    /// <summary>Xaridlar tarixi. <c>search</c> — ism, email, telefon yoki user id.</summary>
    [HttpGet("market/purchases")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<AdminMarketPurchaseDto>>>(200)]
    public Task<Wrapper> GetMarketPurchases([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] long? itemId, [FromQuery] string? search, [FromQuery] DataQueryRequest q) =>
        marketAdminService.GetPurchases(from, to, itemId, search, q);

    #endregion

    #region Referral (do'stni taklif qilish)

    /// <summary>Voronka (kod → do'st → faol → to'lov), tushum, chegirma, premiumlar va kunma-kun grafik.</summary>
    [HttpGet("referrals/summary")]
    [ProducesResponseType<WrapperGeneric<AdminReferralSummaryDto>>(200)]
    public async Task<Wrapper> GetReferralSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        (await referralAdminService.GetSummary(from, to), 200);

    /// <summary>
    /// Taklif qiluvchilar reytingi. <c>sort</c>: invited | activated | paid | revenue | recent.
    /// </summary>
    [HttpGet("referrals/referrers")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<AdminReferrerRowDto>>>(200)]
    public Task<Wrapper> GetReferrers([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? search, [FromQuery] string? sort, [FromQuery] DataQueryRequest q) =>
        referralAdminService.GetReferrers(from, to, search, sort, q);

    /// <summary>Taklif qiluvchining kartasi: umr bo'yi natijalar, premiumlar, kim taklif qilgani.</summary>
    [HttpGet("referrals/referrers/{userId:long:min(1)}")]
    [ProducesResponseType<WrapperGeneric<AdminReferrerDetailDto>>(200)]
    public async Task<Wrapper> GetReferrer(long userId) =>
        (await referralAdminService.GetReferrer(userId), 200);

    /// <summary>
    /// Kim kimni taklif qilgani. <c>status</c>: Joined | Active | Paid; <c>referrerId</c> — bitta
    /// taklif qiluvchining do'stlari (davr berilmasa butun vaqt).
    /// </summary>
    [HttpGet("referrals")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<AdminReferralRowDto>>>(200)]
    public Task<Wrapper> GetReferrals([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? status, [FromQuery] long? referrerId, [FromQuery] string? search,
        [FromQuery] DataQueryRequest q) =>
        referralAdminService.GetReferrals(from, to, status, referrerId, search, q);

    #endregion

    #region Subscriptions (admin-managed)

    [HttpGet("subscriptions/{userId:long:min(1)}")]
    [ProducesResponseType<WrapperGeneric<GetSubscriptionDto>>(200)]
    public async Task<Wrapper> GetUserSubscription(long userId) =>
        (await subscriptionService.GetByUserId(userId), 200);

    [HttpPost("subscriptions")]
    [ProducesResponseType<WrapperGeneric<GetSubscriptionDto>>(200)]
    public async Task<Wrapper> CreateOrUpdateSubscription([FromBody] CreateOrUpdateSubscriptionDto dto) =>
        (await subscriptionService.CreateOrUpdate(dto), 200);

    [HttpDelete("subscriptions/{userId:long:min(1)}")]
    public async Task<Wrapper> DeleteSubscription(long userId)
    {
        await subscriptionService.Delete(userId);
        return 200;
    }

    #endregion
}