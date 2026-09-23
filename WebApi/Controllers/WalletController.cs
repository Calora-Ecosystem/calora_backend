using BRB.Core.Common.Models;
using Core.Attributes;
using Core.Entities.Coins.Enum;
using Core.Enums;
using Core.Services.Coins;
using Core.Services.Coins.Contracts;
using Microsoft.AspNetCore.Mvc;
using ResultWrapper.Library;
using WebCore.Controller;

namespace WebApi.Controllers;

/// <summary>
/// Profil → Hamyon: coin balansi, Calora → coin almashtirish, marketplace va coin reytingi.
/// </summary>
[ApiController]
[Route("wallet")]
[RoleAuthorize(EnumRole.User)]
public class WalletController(CoinService coinService) : AuthorizedController
{
    [HttpGet]
    [ProducesResponseType<WrapperGeneric<WalletDto>>(200)]
    public async Task<Wrapper> GetWallet() => (await coinService.GetWallet(this.UserId), 200);

    [HttpGet("transactions")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<CoinTransactionDto>>>(200)]
    public async Task<Wrapper> GetTransactions([FromQuery] DataQueryRequest q, [FromQuery] EnumCoinTxType? type) =>
        await coinService.GetTransactions(this.UserId, q, type);

    /// <summary>Calora'ni coinga almashtirish (<c>caloraPerCoin</c> Calora = 1 coin).</summary>
    [HttpPost("exchange")]
    [ProducesResponseType<WrapperGeneric<ExchangeResultDto>>(200)]
    public async Task<Wrapper> Exchange([FromBody] ExchangeCaloraDto dto) =>
        (await coinService.Exchange(this.UserId, dto), 200);

    /// <summary>Coin reytingi (davr ichida ishlab topilgan coinlar). Davr berilmasa — butun vaqt.</summary>
    [HttpGet("ranking")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<GetCoinStatDto>>>(200)]
    public Task<Wrapper> Ranking([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] DataQueryRequest q) => coinService.Ranking(from, to, q);

    #region Marketplace

    [HttpGet("market")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<MarketItemDto>>>(200)]
    public async Task<Wrapper> GetMarketItems([FromQuery] DataQueryRequest q,
        [FromQuery] EnumMarketCategory? category) =>
        await coinService.GetMarketItems(q, category);

    [HttpPost("market/{marketItemId:long:min(1)}/purchase")]
    [ProducesResponseType<WrapperGeneric<PurchaseResultDto>>(200)]
    public async Task<Wrapper> Purchase(long marketItemId) =>
        (await coinService.Purchase(this.UserId, marketItemId), 200);

    [HttpGet("purchases")]
    [ProducesResponseType<WrapperGeneric<IEnumerable<MarketPurchaseDto>>>(200)]
    public async Task<Wrapper> GetPurchases([FromQuery] DataQueryRequest q) =>
        await coinService.GetPurchases(this.UserId, q);

    #endregion

    #region Marketplace (admin)

    [HttpGet("market/items")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    [ProducesResponseType<WrapperGeneric<IEnumerable<MarketItemDto>>>(200)]
    public async Task<Wrapper> GetAllMarketItems([FromQuery] DataQueryRequest q,
        [FromQuery] EnumMarketCategory? category) =>
        await coinService.GetMarketItems(q, category, onlyActive: false);

    [HttpPost("market/items")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    [ProducesResponseType<WrapperGeneric<MarketItemDto>>(200)]
    public async Task<Wrapper> CreateOrUpdateMarketItem([FromBody] CreateOrUpdateMarketItemDto dto) =>
        (await coinService.CreateOrUpdateMarketItem(dto), 200);

    [HttpDelete("market/items/{marketItemId:long:min(1)}")]
    [RoleAuthorize(EnumRole.SuperAdmin)]
    public async Task<Wrapper> DeleteMarketItem(long marketItemId)
    {
        await coinService.DeleteMarketItem(marketItemId);
        return 200;
    }

    #endregion
}
