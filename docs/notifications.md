# WBS 3.6 — Notifications

تضيف هذه الخطوة إشعارات داخل التطبيق دائمة وموجهة للمستخدم، مع بث فوري اختياري عبر SignalR. تبقى HTTP مسؤولة عن القراءة وتأكيد الحالة الدائمة، فيما ينقل Hub الأحداث الفورية فقط ولا يصل إلى EF Core أو Repositories.

## النموذج والتدفق

يحفظ كيان `Notification` المستلم ونوع الإشعار والعنوان والمحتوى ومرجع مصدر عام عبر `SourceType` و`SourceId`، إضافة إلى وقت الإنشاء والقراءة. يدعم ذلك ربط الإشعار بالملخصات والمهام دون جعل النموذج مرتبطًا بميزة واحدة. تفهرس قاعدة البيانات قائمة المستخدم وفق `(RecipientUserId, ReadAtUtc, CreatedAtUtc)`، كما تفهرس المصدر لتسهيل التتبع والتدقيق.

| العملية | Use Case | الضابط |
|---|---|---|
| عرض القائمة | `GetNotificationsQuery` | يعرض فقط إشعارات المستخدم الحالي مرتبة من الأحدث إلى الأقدم. |
| تأكيد القراءة | `MarkNotificationReadCommand` | يتحقق من المالك، ويحفظ القراءة قبل نشر `notificationRead`. |
| توفر مسودة ملخص | `GenerateMeetingSummaryCommand` | ينشئ إشعار `MeetingSummaryReady` للمشاركين ويحفظه قبل `notificationCreated`. |
| اعتماد الملخص والمهام | `ApproveMeetingSummaryCommand` | ينشئ إشعار `ActionItemAssigned` للمسؤول، وينشر `actionItemsCreated` بعد الحفظ فقط. لا يغير منطق ADR-002. |

## SignalR

المسار هو `/hubs/notifications` ويستلزم JWT. ينضم الاتصال إلى مجموعة موجهة للمستخدم `user:{userId}` عند الاتصال أو عند استدعاء `SubscribeUserNotifications()`. أحداث الخادم هي `notificationCreated` و`notificationRead` و`actionItemsCreated`. يبقى `NotificationsHub` خاليًا من منطق الأعمال والبيانات؛ وظيفته الوحيدة إدارة مجموعة SignalR الموثقة.

## نقطة التمديد المستقبلية

تعرّف Application `IOutboundNotificationService` مع `OutboundNotificationRequest` كعقد امتداد مستقبلي. لا يسجل هذا التغيير محول Push أو Email ولا يستورد MailKit أو Microsoft Graph أو SDK خارجي. يضاف التنفيذ لاحقًا داخل Infrastructure بعد اعتماد قناة الإرسال والأسرار وسياسة إعادة المحاولة، من دون تغيير نموذج الإشعار أو Handlers الحالية.

## واجهات HTTP

| الطريقة والمسار | النتيجة |
|---|---|
| `GET /api/v1/notifications` | قائمة الإشعارات الدائمة للمستخدم الموثق. |
| `POST /api/v1/notifications/{notificationId}/read` | تأكيد القراءة للمستلم فقط مع غلاف API الموحد. |
