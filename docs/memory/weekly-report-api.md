---
name: weekly-report-api
description: Haftalik hisobot API (GET reports/weekly) — 7 kunlik kkal/makros/qadam/suv, top taom, streak, badge'lar
type: reference
---

# Haftalik hisobot API

`ReportController` (`reports`), `WeeklyReportService`. Jadval yo'q — har so'rovda hisoblanadi.

- `GET reports/weekly?weekStart=yyyy-MM-dd` — shu sana tushgan hafta (Du–Ya). Berilmasa — o'tgan hafta (server vaqti).
- Kkal/makros `daily_menus` dan, `FoodService.Summary` formulasi bilan (metrika / taom og'irligi (default 400) * yeyilgan og'irlik).
- Qadam va suv `user_dailies` dan; coin — `coin_transactions` (`Steps`, RefId = yyyyMMdd).
- `inNorm`: ovqat yozilgan va kkal normaning 75%–110% oralig'ida.
- Ovqat o'rtachasi yozilgan kunlar bo'yicha; qadam/suv o'rtachasi 7 kun bo'yicha.
- `streak` — hafta oxiridan orqaga ketma-ket ovqat yozilgan kunlar (365 kungacha).
- `kcalAvgChangePercent`/`stepsChangePercent` — o'tgan haftaga nisbatan, o'tgan haftada data bo'lmasa null.
- `badges` (mobile lokalizatsiya kalitlari): `perfect_week` (≥5 kun normada), `consistent` (7 kun yozilgan), `step_master` (≥70k qadam), `protein_pro` (o'rtacha protein ≥90% norma), `hydrated` (≥5 kun suv normasi).
- `UserNorms` so'rovi faqat `user_norms` jadvalini o'qiydi. `!(x is UserDaily)` filtri EF'da tarjima qilinmaydi (500 bergan edi) — qo'shmang.
- `activeDays` — ovqat, qadam yoki suv yozilgan kunlar; `stepDaysInNorm`/`waterDaysInNorm` — normaga yetilgan kunlar. Mobile hisobotni faqat hafta butunlay bo'sh bo'lsa ko'rsatmaydi (faqat qadam bo'lsa ham ko'rsatadi).
- `body` — `user_extras` dan joriy/boshlang'ich vazn, bo'y, BMI, purpose; `targetWeight` = `norms.weight` (user_norms Weight). Vazn tarixi yo'q: `user_dailies.Weight` progress (abs farq) saqlaydi, tarix emas.
- `coins` — `steps` (RefId bo'yicha), `referral`, `earned` (barcha kirim), `spent` (chiqim, musbat), `balance` (hozirgi, `coin_wallets`). Qadamdan boshqalari `CreatedAt` bo'yicha.
- `course` — `course_item_states` `UpdatedAt` hafta ichida: `lessons`/`exercises`/`workouts`.
- `friendsInvited` — `referrals` (ReferrerId, CreatedAt hafta ichida); `stepGroups` — a'zo bo'lgan qadam guruhlari (hozirgi).
- Badge `step_goal` — ≥5 kun qadam normasi.
