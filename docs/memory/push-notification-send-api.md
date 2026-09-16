---
name: push-notification-send-api
description: SuperAdmin push bildirishnomalarni userlar ro'yxatiga yoki barcha userlarga yuborish API-si
type: reference
---

# Push Notification Send API

SuperAdmin tomonidan foydalanuvchilar ro'yxatiga yoki barcha foydalanuvchilarga push notification yuborish uchun API.

- **Endpoint**: `POST /notifications/send` (`NotificationController`, ruxsat: `EnumRole.SuperAdmin`).
- **DTO**: `SendPushNotificationDto : PushNotificationDto`:
  - `allUsers` (bool, default false)
  - `userIds` (List<long>?)
  - `PushNotificationDto` dan meros olingan: `title`, `description`, `image`, `meta`, `scheduled`, `mealGateMenu`, `userId`
- **Jadval**: `notifications` jadvaliga `PushNotification` entity-si sifatida yoziladi (batch size 1000).
- **Worker & Firebase Optimizatsiyasi**:
  - `SendBatchPush` orqali faol tokenlar 500 tadan bo'lib Firebase Multicast orqali yuboriladi (1 user = 1 token).
  - Yuborishda 10 daqiqalik recurring job kutmasdan, Hangfire navbatiga darhol `SendBatchPush` qo'yiladi.
  - Tokeni yo'q foydalanuvchilar uchun Firebase-ga bormasdan `SentAt = now` qilib belgilanadi.
