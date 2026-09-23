---
name: wallet-coins-referral-api
description: Qadamdan yig'iladigan coin hamyoni (1000 qadam = 1 coin), marketplace, coin reytingi va referral (5 do'st = 1 oy premium, 10% chegirma) API-lari
type: reference
---

# Hamyon, marketplace, coin reytingi, referral

Sozlamalar: `Coins` bo'limi (`CoinConfig`, hammasining default'i bor).

- **Coin faqat qadamdan yig'iladi** (Calora va almashtirish olib tashlangan): har `StepsPerCoin` (1000) qadam = 1 coin, kuniga ko'pi bilan `MaxDailyCoins` (22). `CoinsEarnStartDate` va user yaratilgan sanadan keyingi kunlar hisoblanadi.
- Sinxron: `users/dailies` (Step) saqlanganda va `GET wallet`da `CoinService.SyncStepCoins` — kuniga bitta `coin_transactions` yozuvi (`type=Steps`, `ref_id=yyyyMMdd`, `title=coin_tx_daily_steps`), faqat oshadi; hamyon qatori `FOR UPDATE` bilan qulflanadi. `(user_id, type, ref_id)` unique.
- Jadvallar: `coin_wallets` (balance, total_earned, total_spent), `coin_transactions`, `market_items`, `market_purchases`, `referrals`, `referral_premium_grants`, `users.referral_code`.
- Balans atomik `ExecuteUpdate ... where balance >= price` bilan kamayadi.

## Hamyon (`wallet`)
- `GET wallet` (balance, todayCoins, stepsPerCoin, maxDailyCoins), `GET wallet/transactions?type=`
- `GET wallet/ranking?from&to` — davrda ishlab topilgan coinlar, javob shakli `users/steps/stat` bilan bir xil (`user, sum, index`).
- `GET wallet/market?category=`, `POST wallet/market/{id}/purchase`, `GET wallet/purchases`; admin: `GET/POST wallet/market/items`, `DELETE wallet/market/items/{id}`.
- Mukofot turlari: `PremiumDays` (`GrantPremiumDays`, source=Coins, `requiresTokenRefresh`), `AiScans`, `Coupon` (user uchun bir martalik `coupons`), `Voucher`.
- Do'kon faqat Premium tariflar (migration `MarketOnlyPremiumTariffs`): `mi_premium_7` 150→7 kun, `mi_premium_30` 300→30 (popular), `mi_premium_75` 600→75, `mi_premium_120` 900→120. Xarid `GrantPremiumDays` (source=Coins) — faol premium bo'lsa muddat ustiga qo'shiladi; muddat tugasa `expire_granted_subscriptions` job o'chiradi. Boshqa mahsulotlar is_active=false.

## Referral (`referrals`)
1. `GET referrals/me` → code, invited, active, friendsGoal (5), premiumDays (30), progressFriends, friendsLeft, premiumsEarned, referredBy, canApplyCode, discountPercent, hasDiscount.
2. `POST referrals/apply {code}` — user do'stining kodini tasdiqlaydi (bir marta, o'z kodi va A↔B taqiqlangan). `ReferralApplyWindowDays` = 0 (default) — istalgan user, eski userlar ham. Mobile'da kod faqat Profil → Invite friends sahifasida kiritiladi (onboarding'da emas).
3. Do'st **faol** bo'ladi (`referrals.qualified_at`) profil/onboarding saqlanganda (`UserService.CreateOrUpdateExtra` → `ReferralService.TryQualify`). Kod onboarding'dan keyin kiritilsa — darhol.
4. Har `ReferralPremiumFriends` (5) ta faol do'st → taklif qiluvchiga `ReferralPremiumDays` (30) kun premium (source=Referral). `referral_premium_grants (referrer_id, milestone)` unique — ikki marta berilmaydi.
5. `GET referrals/invited` — do'stlar ro'yxati, `status`: `Joined` (ro'yxatdan o'tdi) / `Active` (ilovaga kirdi).
6. Taklif qilingan user birinchi premium xaridida `ReferredDiscountPercent` (10%) chegirma: `billing/orders/subscription/plans/{plan}` → `referralDiscountPercent`, `discountedFee`; order `referral_discount` ustunida. Faqat Click/Payme — IAP narxini store belgilaydi. Chegirma to'lov tasdiqlanganda ishlatilgan bo'ladi (`ReferralDiscountService`).
7. Push: kod tasdiqlandi / do'st faol bo'ldi / premium berildi.
8. Kodlar `referral_codes` jadvalida (`CALORA-XXXXXX`): `POST referrals/code` har ulashishda yangi kod beradi, eski kodlar ham amal qiladi; `referrals/me` oxirgisini qaytaradi. `referrals.code` — qaysi kod tasdiqlangani. `users.referral_code` eski, faqat migratsiya uchun.
