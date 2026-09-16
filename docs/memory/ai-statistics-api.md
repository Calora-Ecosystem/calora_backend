---
name: ai-statistics-api
description: AI (food_recognition) usage, cost, token, and premium adoption statistics API endpoint
type: reference
---

# AI Statistics API

Provides AI analytics and cost metrics based on `EventLogs` (`source = "ai.food_recognition"`) and `Subscriptions`.

- **Endpoint**: `GET /dashboard/ai/statistics` (under `DashboardController`, requires `EnumRole.SuperAdmin`).
- **Query params**: `days` (default 28), `from`, `to`.
- **Pricing rates** (`gemini-2.5-flash`):
  - Prompt: $0.30 per 1M tokens ($0.00000030/token)
  - Candidate: $2.50 per 1M tokens ($0.00000250/token)
- **Key Metrics**:
  - `totalAiUsers`, `totalRequests`, `avgRequestsPerUser`, `avgRequestsPerDay`
  - `minRequestsPerUser` (min requests single user made), `maxRequestsPerUser` (max requests single user made)
  - `totalCostUsd`, `costPerUserUsd`, `costPerRequestUsd`
  - `totalPremiumUsers`, `activeAiPremiumUsers`, `aiAdoptionRatePercent`
  - `dailyTrend`, `topUsers`
