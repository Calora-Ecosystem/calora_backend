using System.Data;
using BRB.Core.Common.Models;
using Core.Services.Billing.Exceptions;
using BRB.Core.EF.Attributes;
using BRB.Core.EF.Extensions;
using Core.Brokers.DbContext;
using Core.Entities.Billing;
using Core.Services.Billing.Contracts;
using Microsoft.EntityFrameworkCore;
using ResultWrapper.Library;

namespace Core.Services.Billing;

[Injectable]
public class CouponService(AppDbContext context)
{
    public async Task<Wrapper> GetAll(DataQueryRequest q)
    {
        return await context.Coupons
            .Select(x => new GetCouponsDto
            {
                Id = x.Id, Usages = x.Usages, Code = x.Code,
                OneTime = x.OneTime,
                ExpireAt = x.ExpireAt,
                AllowedUserIds = x.AllowedUserIds,
                IsActive = x.IsActive,
                CreatedByUserId = x.CreatedByUserId,
                CreatedByName = x.CreatedByUser != null ? x.CreatedByUser.Name : null,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<GetCouponByIdDto> GetById(long id)
    {
        return await context.Coupons
            .Select(x => new GetCouponByIdDto
            {
                Id = x.Id, Usages = x.Usages, Code = x.Code,
                OneTime = x.OneTime,
                ExpireAt = x.ExpireAt,
                Amount = x.Amount,
                AllowedUserIds = x.AllowedUserIds,
                IsActive = x.IsActive,
                CreatedByUserId = x.CreatedByUserId,
                CreatedByName = x.CreatedByUser != null ? x.CreatedByUser.Name : null,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw new CouponNotFoundException();
    }

    public async Task<List<MyFamilyCouponDto>> GetMyFamilyCoupons(long userId)
    {
        var now = DateTime.Now;

        var coupons = await context.Coupons
            .AsNoTracking()
            .Where(x => x.CreatedByUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync();

        if (coupons.Count == 0)
            return [];

        var couponIds = coupons.Select(x => x.Id).ToList();

        var usages = await context.CouponUsages
            .AsNoTracking()
            .Where(x => couponIds.Contains(x.CouponId))
            .OrderByDescending(x => x.CreatedAt)
            .Join(context.Users, usage => usage.UserId, user => user.Id, (usage, user) => new
            {
                usage.CouponId,
                usage.UserId,
                UserName = user.Name,
                UserPhone = user.Phone,
                usage.CreatedAt
            })
            .ToListAsync();

        return coupons.Select(coupon =>
        {
            var couponUsages = usages.Where(u => u.CouponId == coupon.Id).ToList();
            var firstUsage = couponUsages.FirstOrDefault();

            var isRedeemed = coupon.Usages > 0 || couponUsages.Count > 0;
            var isExpired = coupon.ExpireAt.HasValue && coupon.ExpireAt.Value < now;

            var status = isRedeemed
                ? "Redeemed"
                : (isExpired || !coupon.IsActive)
                    ? "Expired"
                    : "Active";

            return new MyFamilyCouponDto
            {
                Id = coupon.Id,
                Code = coupon.Code,
                Amount = coupon.Amount,
                IsActive = coupon.IsActive,
                ExpireAt = coupon.ExpireAt,
                CreatedAt = coupon.CreatedAt,
                UsedAt = firstUsage?.CreatedAt,
                UsedByUserIds = couponUsages.Select(u => u.UserId).ToList(),
                UsedByUsers = couponUsages.Select(u => new FamilyCouponUserDto
                {
                    UserId = u.UserId,
                    Name = u.UserName,
                    Phone = u.UserPhone
                }).ToList(),
                UsedByUserId = firstUsage?.UserId,
                UsedByName = firstUsage?.UserName,
                Status = status
            };
        }).ToList();
    }

    public async Task<Wrapper> GetCouponUsages(long couponId, DataQueryRequest q)
    {
        return await context.CouponUsages
            .Where(x => x.CouponId == couponId)
            .Join(context.Users, usage => usage.UserId, user => user.Id, (usage, user) => new GetCouponUsagesDto
            {
                OrderId = usage.OrderId, UserName = user.Name, CreatedAt = usage.CreatedAt,
                Amount = usage.Amount / 100d
            })
            .GetByDataQueryAsync(q);
    }

    public async Task<CheckCouponDto> CheckCoupon(long userId, long couponId)
    {
        var coupon = await context.Coupons.AsNoTracking().GetByIdOrThrowsNotFoundException(couponId);
        return await CheckCoupon(userId, coupon.Code);
    }

    public async Task<CheckCouponDto> CheckCoupon(long userId, string code)
    {
        var coupon =
            await context.Coupons
                .AsNoTracking()
                .FirstOrDefaultAsync(x => EF.Functions.ILike(x.Code, code) && x.IsActive
                                                                           && (!x.OneTime || x.Usages == 0)
                                                                           && (!x.ExpireAt.HasValue ||
                                                                               x.ExpireAt.Value >= DateTime.Now)
                )
            ?? throw new CouponNotFoundException();

        if (coupon.AllowedUserIds != null && !coupon.AllowedUserIds.Contains(userId))
            throw new CouponNotFoundException();

        if (coupon.CreatedByUserId.HasValue && coupon.CreatedByUserId.Value == userId)
            throw new CouponSelfUseException();

        return new CheckCouponDto
        {
            Id = coupon.Id,
            ExpireAt = coupon.ExpireAt,
            IsActive = coupon.IsActive,
            Amount = coupon.Amount
        };
    }

    public async Task CreateOrUpdate(CreateOrUpdateCouponDto dto)
    {
        await context.Transactional(async () =>
        {
            if (dto.ExpireAt.HasValue && dto.ExpireAt.Value <= DateTime.Now)
                throw new CouponExpireAtInvalidException();

            if (dto.AllowedUserIds != null)
            {
                var existedUserIdsCount = await context.Users.CountAsync(x => dto.AllowedUserIds.Contains(x.Id));

                if (existedUserIdsCount != dto.AllowedUserIds.Count)
                    throw new CouponUsersNotFoundException();
            }

            var coupon = dto.Id.HasValue
                ? await context.Coupons
                      .FirstOrDefaultAsync(x =>
                          x.Id == dto.Id.Value && EF.Functions.ILike(x.Code, dto.Code))
                  ?? throw new CouponNotFoundException()
                : null;

            if (coupon is null)
            {
                if (await context.Coupons.AnyAsync(x => EF.Functions.ILike(x.Code, dto.Code)))
                    throw new CouponCodeAlreadyExistsException();

                coupon = new Coupon()
                {
                    Code = dto.Code.ToUpperInvariant(),
                };
            }


            if (coupon.Id != 0 && coupon.Usages > 1 && dto.OneTime)
            {
                throw new CouponAlreadyUsedException();
            }

            coupon.ExpireAt = dto.ExpireAt;
            coupon.AllowedUserIds = dto.AllowedUserIds;
            coupon.OneTime = dto.OneTime;
            coupon.IsActive = dto.IsActive;
            coupon.Amount = dto.Amount;

            context.Update(coupon);
            await context.SaveChangesAsync();
        });
    }

    public async Task<long> ApplyCoupon(long amount, long orderId, long userId, long couponId)
    {
        await context.Transactional(async () =>
        {
            var coupon = await context.Coupons.GetByIdOrThrowsNotFoundException(couponId);
            await CheckCoupon(userId, coupon.Code);

            coupon.Usages++;
            
            if (coupon is { OneTime: true, Usages: >= 1 })
            {
                coupon.IsActive = false;
            }

            context.Update(coupon);
            context.CouponUsages.Add(new CouponUsage()
            {
                CouponId = coupon.Id,
                Amount = amount,
                OrderId = orderId,
                UserId = userId
            });

            await context.SaveChangesAsync();

            amount -= coupon.Amount;

            if (amount < 0)
                amount = 0;
        });

        return amount;
    }
}