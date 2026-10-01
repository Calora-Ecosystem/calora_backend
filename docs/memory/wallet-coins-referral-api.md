---
name: wallet-coins-referral-api
description: Qadamdan yig'iladigan coin hamyoni (1000 qadam = 1 coin), marketplace, coin reytingi va referral (5 do'st = 1 oy premium, 10% chegirma) API-lari
type: reference
---

# Hamyon, marketplace, coin reytingi, referral

Sozlamalar: `Coins` bo'limi (`CoinConfig`, hammasining default'i bor).

- **Coin faqat qadamdan yig'iladi** (Calora va almashtirish olib tashlangan): har N qadam = 1 coin, kunlik limit bilan. **Coin hisoblash kuni** va user yaratilgan sanadan keyingi kunlar hisoblanadi.
- **Coin hisoblash kuni dashboard'dan** (`coin_settings` bitta qator, `CoinRuleService.GetEarnStartDate`; qator yo'q bo'lsa `CoinConfig.CoinsEarnStartDate`). Kelajak kun qo'yilsa o'sha kungacha hech kimga coin yozilmaydi (release kuniga oldindan qo'yish). `POST dashboard/coins/earn-start {earnStartDate, resetCoins}`: `resetCoins=true` — **barcha** `coin_transactions` o'chiriladi va hamma `coin_wallets` 0 (balance/total_earned/total_spent); `market_purchases` va berilgan Premium qoladi; `coin_resets` jurnaliga yoziladi. `lock table coin_wallets in exclusive mode` bilan; SyncStepCoins wallet qulfidan keyin kunni qayta o'qiydi (`fresh`) — reset paytidagi parallel sinxron eski kunlarni qayta yozmaydi. Reset + o'tgan kun = o'sha kundan bugungacha coin qayta hisoblanadi (SyncStepCoins hamma kunni qayta hisoblaydi). `GET dashboard/coins/earn-start` — kun, coinlar holati, `stepCoinsBeforeStart`, reset tarixi. `GET wallet` → `earnStartDate`.
- **Qoida dashboard'dan boshqariladi** (`coin_rules` jadvali, `CoinRuleService`): har qoida `effective_from` kunidan amal qiladi; **har kun o'sha kunda amal qilgan qoida bilan** hisoblanadi (`CoinRuleSet.For(day)`), shuning uchun yangi qoida o'tgan kunlarga orqaga qarab coin bermaydi (SyncStepCoins hamma kunni qayta hisoblaydi — tarixsiz qoida 1000→500 bo'lsa hammaga eski kunlar uchun ikki baravar coin berardi). Qoida yo'q kunlar uchun `CoinConfig.StepsPerCoin/MaxDailyCoins` (1000/22). Faqat bugungi/kelajakdagi qoidani saqlash/o'chirish mumkin (`coin_rule_past_date`, `coin_rule_locked`); bir kunga bitta qoida (unique) — saqlash upsert.
- Sinxron: `users/dailies` (Step) saqlanganda va `GET wallet`da `CoinService.SyncStepCoins` — kuniga bitta `coin_transactions` yozuvi (`type=Steps`, `ref_id=yyyyMMdd`, `title=coin_tx_daily_steps`), faqat oshadi; hamyon qatori `FOR UPDATE` bilan qulflanadi. `(user_id, type, ref_id)` unique.
- Jadvallar: `coin_wallets` (balance, total_earned, total_spent), `coin_transactions`, `market_items`, `market_purchases`, `referrals`, `referral_premium_grants`, `users.referral_code`.
- Balans atomik `ExecuteUpdate ... where balance >= price` bilan kamayadi.

## Hamyon (`wallet`)
- `GET wallet` (balance, todayCoins, stepsPerCoin, maxDailyCoins — **bugungi qoida**, `nextRule` — kelajakdagi qoida {stepsPerCoin,maxDailyCoins,effectiveFrom} yoki null), `GET wallet/transactions?type=`
  — qadam yozuvlarida `stepDate` (coin qaysi kun uchun, `ref_id`dan) va `steps` (o'sha kungi qadam) ham bor; `createdAt` — coin yozilgan vaqt (bir necha kun birdan yozilishi mumkin).
- Balans = `CoinsEarnStartDate`dan beri har kun uchun `min(qadam/1000, 22)` yig'indisi — faqat bugungi qadam emas.
- `users/dailies`: `Z` (UTC) bilan kelgan sana server vaqtiga (Asia/Tashkent) o'giriladi, keyin kun olinadi — aks holda 00:00–05:00 dagi qadam kechagi kunga tushardi.
- `GET wallet/ranking?from&to` — davrda ishlab topilgan coinlar, javob shakli `users/steps/stat` bilan bir xil (`user, sum, index`).
- Reyting davri (`CoinService.RankedEarnings` / `EarnedInPeriod`): qadam coini **qadam kuni** (`ref_id`) bo'yicha, boshqa kirimlar `created_at` bo'yicha; o'chirilgan userlar kirmaydi. Mobile va dashboard reytingi bir xil hisoblanadi.

## Dashboard: coin reytingi (SuperAdmin, `DashboardController`)
- `from`/`to` — kunlar (ikkalasi ham kiradi); berilmasa `CoinsEarnStartDate`dan bugungacha.
- `GET dashboard/coins/summary` — participants, earned (stepCoins/bonusCoins), spent, avgPerParticipant, balanceInCirculation.
- `GET dashboard/coins/ranking?search&Skip&Take` — rank, user (name/email/phone/photo), earned, stepCoins, bonusCoins, activeDays, maxedDays, balance, totalEarned, totalSpent, lastEarnedAt. `search` (ism/email/telefon/id) faqat filtrlaydi — `rank` umumiy reytingdagi o'rin.
- `GET dashboard/coins/users/{id}` — hamyon, davrdagi rank/participants, bestDay va `days[]` (har kun: steps, stepCoins, bonusCoins, spent, earned; coin ishga tushishidan va ro'yxatdan o'tishdan oldingi hamda kelajak kunlar yo'q, ko'pi bilan 366 kun).
- `GET dashboard/coins/users/{id}/transactions?type` — `wallet/transactions` bilan bir xil.
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

## Dashboard: coin qoidasi, do'kon, referral (SuperAdmin, `DashboardController`)
- Qoida: `GET dashboard/coins/rules` (current, next, history yangidan eskiga — appsettings qoidasi `isDefault`, status Past/Current/Upcoming), `POST dashboard/coins/rules {stepsPerCoin 1..100000, maxDailyCoins 1..1000, effectiveFrom?=bugun, note?}`, `DELETE dashboard/coins/rules/{id}`, `GET dashboard/coins/rules/preview?stepsPerCoin&maxDailyCoins&days=30` — oxirgi kunlar haqiqiy qadamlari bo'yicha joriy vs taklif qilingan qoida (jami/kunlik coin, limitga yetganlar ulushi, faol kunga o'rtacha coin).
- `AdminCoinDayDto` (`coins/users/{id}` days) endi o'sha kungi `stepsPerCoin/maxDailyCoins` ni ham qaytaradi; `maxedDays` kunlik qoida bo'yicha.
- Do'kon (`MarketAdminService`): `GET dashboard/market/items?from&to` (mahsulot + davrdagi xaridlar, coinlar, xaridorlar, `visibleInApp`, `daysToEarn` — faol user joriy qoida bo'yicha necha kunda yig'adi, `coinsPerPremiumDay`), `POST dashboard/market/items` (= `CreateOrUpdateMarketItemDto`), `DELETE dashboard/market/items/{id}` (xarid qilingan bo'lsa `market_item_in_use`), `GET dashboard/market/summary?from&to` (xaridlar, xaridorlar, sarflangan coin, berilgan premium kunlar, takroriy xaridorlar, eng arzon tarifga yetadigan userlar, kunma-kun), `GET dashboard/market/purchases?from&to&itemId&search&Skip&Take`.
- `CreateOrUpdateMarketItem`: PremiumDays 1..3650 kun (`market_item_invalid`); `isPopular=true` shu kategoriyadagi boshqalardan olib tashlanadi (bitta "mashhur"). Mobile faqat `IsActive && Category=Tariff && RewardType=PremiumDays` ni ko'rsatadi, `rewardValue` bo'yicha saralaydi; title — lokalizatsiya kaliti `mi_premium_{days}` (mobile'da kalit bo'lmasa `mi_premium_days` fallback).
- Referral (`ReferralAdminService`, kogorta = davrda kod kiritgan do'stlar; to'lov = kod kiritilgandan keyingi Confirmed orderlar, tiyin→so'm): `GET dashboard/referrals/summary?from&to` (kodlar/ulashganlar, invited/activated/pending/paid, konversiyalar, faollashish soati, birinchi to'lovgacha kun, yangi userlarning referral ulushi, tushum, chegirma, premium grantlar, referral coinlari, `nearMilestone` — premiumga 1 do'st qolganlar, umr bo'yi jami, `program` (appsettings), `days[]`), `GET dashboard/referrals/referrers?from&to&search&sort=invited|activated|paid|revenue|recent&Skip&Take` (reyting, rank — qidiruvdan qat'i nazar), `GET dashboard/referrals/referrers/{id}` (umr bo'yi karta, grantlar, kim taklif qilgan, 90 kunlik grafik), `GET dashboard/referrals?from&to&status=Joined|Active|Paid&referrerId&search&Skip&Take` (kim kimni; `referrerId` bilan davrsiz — butun vaqt). O'chirilgan userlar ham ko'rinadi (`IgnoreQueryFilters`).
