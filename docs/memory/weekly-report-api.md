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
- Vazn hozircha yo'q: `user_dailies.Weight` progress (abs farq) saqlaydi, tarix emas.
