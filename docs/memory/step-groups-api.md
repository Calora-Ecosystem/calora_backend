---
name: step-groups-api
description: Userlar yaratadigan qadam guruhlari (taklif kodi bilan qo'shilish, guruh ichidagi qadam reytingi) API-si
type: reference
---

# Qadam guruhlari API

`StepGroupController` (`step-groups`), `StepGroupService`. Jadvallar: `step_groups` (name, invite_code unique `CAL-XXXXX`, owner_id), `step_group_members` (group_id+user_id unique).

- `GET step-groups?from&to` — mening guruhlarim (memberCount, totalSteps, isOwner).
- `GET step-groups/{id}?from&to` — a'zolar qadam bo'yicha reytingi (`steps, index, isMe, isOwner`). Faqat a'zolar ko'radi (aks holda 404).
- `POST step-groups {name}`, `POST step-groups/join {code}` (qayta join idempotent).
- `DELETE step-groups/{id}` (faqat admin), `POST step-groups/{id}/leave` (admin chiqsa adminlik eng eski a'zoga o'tadi, oxirgi a'zo chiqsa guruh o'chadi), `DELETE step-groups/{id}/members/{userId}` (faqat admin).
- Qadamlar alohida saqlanmaydi — `user_dailies` (`Step`) dan hisoblanadi. `from/to` default — bugun (`users/steps/stat` kabi).
- Limitlar: guruhda 50 a'zo, userda 20 guruh.
