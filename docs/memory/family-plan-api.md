---
name: family-plan-api
description: Oilaviy tarif (2 kishi) — PlanExtra.IsFamily, to'lovda ikkinchi odamga FAMILY-XXXXXX kod, billing/family/redeem bilan 1 oy premium
type: reference
---

# Oilaviy tarif (2 kishi)

- Paket: `plan_extras.is_family = true` — **admin dashboard'da yaratiladi** (Tariflar sahifasida "Oilaviy" belgisi; narx, muddat, faollik shu yerda). Migratsiya narx seed qilmaydi; paket bo'lmasa ilova oilaviy tarifni ko'rsatmaydi. `CreateOrUpdatePlanExtraDto.IsFamily` nullable — yubormagan eski dashboard qiymatni o'zgartirmaydi. Dublikat/IsPopular tekshiruvi oddiy va oilaviy paketlarda alohida.
- Ro'yxat: `GET billing/orders/subscription/plans/Premium` — default **faqat oddiy** paketlar (eski ilovalar oilaviyni "oylik" deb ko'rsatmasin); `?family=true` — faqat oilaviy.
- Sotib olish: oddiy order oqimi (`orders/subscription`), lekin **faqat Click/Payme** — IAP bilan `family_plan_store_unavailable` (store'da oilaviy mahsulot yo'q; IAP orqali oylik narx to'lanib kod ketardi).
- To'lov qabul qilinganda (`OrderService.AcceptSubscriptionPaymentAsync`): xaridor odatdagidek premium bo'ladi va shu order uchun bitta `family_codes` qatori (`FAMILY-XXXXXX`, `months` = paket muddati, `expire_at` = +30 kun) obuna bilan **bitta SaveChanges**da yoziladi. `order_id` unique — provayder qayta yuborsa ikkinchi kod chiqmaydi. Egasiga push (kod matni bilan).
- `GET billing/family/codes` — egasining kodlari (eng yangisi birinchi): code, months, status (Active / Redeemed / Expired), expireAt, redeemedAt, redeemedBy (ism).
- `POST billing/family/redeem {code}` — ikkinchi odam: kod atomik band qilinadi (`ExecuteUpdate ... where redeemed_by_id is null`), `SubscriptionService.GrantPremiumDays(source=Family)`; javob `{ownerName, months, endsAt, requiresTokenRefresh: true}`. Xatolar: `family_code_not_found` (404), `family_code_used`, `family_code_expired`, `family_code_self`. Egasiga "kod faollashdi" push.
- `subscriptions.source = Family` (5) — muddati o'tsa `expire_granted_subscriptions` job o'chiradi (Coins/Referral kabi). `GET billing/subscription/my` → `isFamily` (oxirgi sotib olingan paket oilaviymi).
- Mobile: Tariflar → Oila → PremiumSheet(family) → to'lovdan keyin kod oynasi (nusxa/ulashish); Profil → Obuna'da kod kartasi va "Oila kodini kiritish"; promo-kod maydoni ham `FAMILY-` kodini qabul qiladi.
