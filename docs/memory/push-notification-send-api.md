---
name: push-notification-send-api
description: SuperAdmin push bildirishnomalarni userlar ro'yxatiga yoki barcha userlarga yuborish API-si
type: reference
---

# Push Notification Send API

SuperAdmin tomonidan foydalanuvchilar ro'yxatiga yoki barcha foydalanuvchilarga push notification yuborish uchun API.

- **Endpoint**: `POST /notifications/batch` (`NotificationController`, ruxsat: `EnumRole.SuperAdmin`).
- **DTO**: `BatchPushNotificationDto : BasePushNotificationDto`:
  - `title` (string, required, max 200)
  - `description` (string?, max 500)
  - `image` (string?)
  - `meta` (Dictionary<string, string>?)
  - `scheduled` (DateTime?)
  - `mealGateMenu` (EnumMenu?)
  - `allUsers` (bool, default false)
  - `userIds` (List<long>?)
- **Jadval**: `notifications` jadvaliga `PushNotification` entity-si sifatida yoziladi (batch size 1000).
- **Worker & Firebase Optimizatsiyasi**:
  - `SendBatchPush` orqali faol tokenlar 500 tadan bo'lib Firebase Multicast orqali yuboriladi (1 user = 1 token).
  - Yuborishda 10 daqiqalik recurring job kutmasdan, Hangfire navbatiga darhol `SendBatchPush` qo'yiladi.
  - Tokeni yo'q foydalanuvchilar uchun Firebase-ga bormasdan `SentAt = now` qilib belgilanadi.
- **Indekslar (Migration)**:
  - `ix_push_notifications_enqueued_at_null`: `(scheduled, created_at) WHERE enqueued_at IS NULL` (Worker uchun partial index).
  - `ix_push_notifications_user_id_sent_at`: `(user_id, sent_at)` (Foydalanuvchi bildirishnomalar ro'yxati).
  - `ix_notifications_user_id_has_read`: `(user_id, has_read)` (O'qilmagan xabarlar hisobi).
