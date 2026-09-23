---
name: free-ai-quota-api
description: 5 ta bepul AI (skan+ovoz) limiti, obuna holati, obuna manbalari va RevenueCat webhook qoidalari
type: reference
---

# Bepul AI limiti, obuna holati va obuna muddati

## Bepul AI limiti
- `POST food/recognization` premium bo'lmagan userga umr bo'yi `AiQuota:FreeLimit` (5) ta so'rov beradi; rasm va ovoz bitta hovuzdan. Faqat bo'sh bo'lmagan natija yechiladi. Tugasa **403 `ai_free_limit_exceeded`**.
- `GET food/recognization/quota` → `{isPremium, unlimited, limit, used, remaining}`. Jadval `user_ai_quotas` (bonus marketplace `AiScans`dan).

## Obuna
- `GET billing/subscription/my` → plan, isPremium, source, startsAt/endsAt, daysLeft, provider, autoRenew, nextPaymentAt, aiQuota.
- **Premium = `IsActive && plan != Free`** — JWT `plan` claim, AI limit va `subscription/my` bir xil qoidada. `EndsAt` claim'da TEKSHIRILMAYDI (IAP renewal'lar tarixan EndsAt'ni yangilamagan — tekshirilsa pullik userlar premiumni yo'qotadi).
- `subscriptions.source`: Payment (default, mavjud qatorlar), Admin, Coins, Referral. Coins/Referral muddati o'tsa `expire_granted_subscriptions` Hangfire job (*/15) o'chiradi; Payment'ni faqat provayder o'chiradi.
- `subscriptions.bonus_days`: IAP obunasi ustiga qo'shilgan referral/coin kunlari. `EndsAt = RevenueCat expiration + bonus_days`; EXPIRATION'da bonus qolsa obuna source=Referral bo'lib davom etadi.
- `AcceptPaymentAsync` idempotent (Confirmed order qayta qabul qilinmaydi) va `subscriptions` qatorini upsert qiladi (user_id unique).

## RevenueCat webhook (`billing/rc`)
- INITIAL_PURCHASE / NON_RENEWING_PURCHASE / turi yo'q → accept; RENEWAL / UNCANCELLATION / PRODUCT_CHANGE → accept + `EndsAt` sinxron (`expiration_at_ms`); EXPIRATION → o'chirish; boshqalari (CANCELLATION, BILLING_ISSUE, TEST...) → e'tiborsiz.
- `billing/test/disable-premium` production'da 404.
