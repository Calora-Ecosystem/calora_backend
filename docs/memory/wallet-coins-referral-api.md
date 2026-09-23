---
name: wallet-coins-referral-api
description: Calora coin hamyoni, marketplace, coin reytingi va referral (5 do'st = 1 oy premium, 10% chegirma) API-lari
type: reference
---

# Hamyon, marketplace, coin reytingi, referral

Sozlamalar: `Coins` bo'limi (`CoinConfig`, hammasining default'i bor).

- **Calora** = qadamlardan yoqilgan kkal (`StepMetricsHelper`, `users/steps/metrics` bilan bir formula). Faqat `CaloraEarnStartDate` va user yaratilgan sanadan keyingi kunlar, kunlik qadam `MaxDailySteps` bilan cheklanadi. `CaloraPerCoin` (1000) Calora = 1 coin.
- Jadvallar: `coin_wallets`, `coin_transactions` (ishorali amount, `title` = mobile lokalizatsiya kaliti), `market_items`, `market_purchases`, `referrals`, `referral_premium_grants`, `users.referral_code`.
- Balans faqat atomik `ExecuteUpdate ... where balance >= price` / `calora_exchanged + spend <= earned` bilan o'zgaradi.

## Hamyon (`wallet`)
- `GET wallet`, `GET wallet/transactions?type=`, `POST wallet/exchange {calora}`
- `GET wallet/ranking?from&to` — davrda ishlab topilgan coinlar, javob shakli `users/steps/stat` bilan bir xil (`user, sum, index`).
- `GET wallet/market?category=`, `POST wallet/market/{id}/purchase`, `GET wallet/purchases`; admin: `GET/POST wallet/market/items`, `DELETE wallet/market/items/{id}`.
- Mukofot turlari: `PremiumDays` (`GrantPremiumDays`, source=Coins, `requiresTokenRefresh`), `AiScans`, `Coupon` (user uchun bir martalik `coupons`), `Voucher`.

## Referral (`referrals`)
1. `GET referrals/me` → code, invited, active, friendsGoal (5), premiumDays (30), progressFriends, friendsLeft, premiumsEarned, referredBy, canApplyCode, discountPercent, hasDiscount.
2. `POST referrals/apply {code}` — user do'stining kodini tasdiqlaydi (bir marta, o'z kodi va A↔B taqiqlangan). `ReferralApplyWindowDays` = 0 (default) — istalgan user, eski userlar ham. Mobile'da kod faqat Profil → Invite friends sahifasida kiritiladi (onboarding'da emas).
3. Do'st **faol** bo'ladi (`referrals.qualified_at`) profil/onboarding saqlanganda (`UserService.CreateOrUpdateExtra` → `ReferralService.TryQualify`). Kod onboarding'dan keyin kiritilsa — darhol.
4. Har `ReferralPremiumFriends` (5) ta faol do'st → taklif qiluvchiga `ReferralPremiumDays` (30) kun premium (source=Referral). `referral_premium_grants (referrer_id, milestone)` unique — ikki marta berilmaydi.
5. `GET referrals/invited` — do'stlar ro'yxati, `status`: `Joined` (ro'yxatdan o'tdi) / `Active` (ilovaga kirdi).
6. Taklif qilingan user birinchi premium xaridida `ReferredDiscountPercent` (10%) chegirma: `billing/orders/subscription/plans/{plan}` → `referralDiscountPercent`, `discountedFee`; order `referral_discount` ustunida. Faqat Click/Payme — IAP narxini store belgilaydi. Chegirma to'lov tasdiqlanganda ishlatilgan bo'ladi (`ReferralDiscountService`).
7. Push: kod tasdiqlandi / do'st faol bo'ldi / premium berildi.
