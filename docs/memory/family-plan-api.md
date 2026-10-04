---
name: family-plan-api
description: Oilaviy tarif (2 kishi) — PlanExtra (Family), to'lovda ikkinchi odam uchun Coupon (FAMILY-XXXXXX), GET billing/family/coupons yoki billing/coupons/my
type: reference
---

# Oilaviy tarif (Family Plan)

- **Paket**: `PlanExtra` ro'yxatida `Plan == EnumSPlans.Family`. Sotib olish: Payme / Click orqali (IAP store mahsuloti yo'q — `FamilyPlanStoreUnavailableException`).
- **To'lov qabul qilinganda** (`OrderService.AcceptSubscriptionPaymentAsync`):
  - Xaridor (owner) obunasi `Family` rejasiga o'tadi va faollashadi.
  - Ikkinchi odam uchun bitta `FAMILY-XXXXXX` kodi bilan 100% chegirmali bir martalik (`OneTime = true`) kupon yaratiladi:
    - `CreatedByUserId = order.UserId`
    - `ExpireAt = now.AddDays(30)`
    - `Amount = orderExtra.PlanExtra.Fee`
  - Kupon egasiga push-bildirishnoma jo'natiladi (`NotifyFamilyCouponIssued`).
- **Kuponlarni ko'rish**:
  - `GET billing/family/coupons` (shuningdek `GET billing/coupons/my` va `GET billing/family/codes`):
    - `[RoleAuthorize(EnumRole.User)]`
    - Egasi o'zi yaratgan barcha kuponlar ro'yxatini oladi (`MyFamilyCouponDto`):
      - `Id`, `Code`, `Amount`, `IsActive`, `ExpireAt`, `CreatedAt`
      - `Status`: `"Active"` | `"Redeemed"` | `"Expired"`
      - `UsedAt`, `UsedByUserId`, `UsedByName`, `UsedByUsers` (ishlatgan odamning ismi va telefoni).
- **Ikkinchi odam kuponni ishlatishi**:
  - Standart obuna oqimi: `GET billing/coupons/check?code=FAMILY-XXXXXX` orqali tekshiradi va `POST billing/orders/subscription` da `CouponId` bilan yuboradi.
  - Kupon 100% chegirma bergani sababli to'lov summasi 0 bo'ladi va obuna bir zumda faollashadi (`order.Amount == 0`).
  - Kupon egasi o'zining kuponini o'zi ishlata olmaydi (`CouponSelfUseException` - `coupon_self_use`).
