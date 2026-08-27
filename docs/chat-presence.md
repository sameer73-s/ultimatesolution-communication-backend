# WBS 3.3 — Chat + Presence

## نطاق المخرج ومعيار القبول

> **المخرج المتوقع:** أوامر واستعلامات CQRS مع FluentValidation لمجال Chat، وHub فعلي للرسائل والكتابة والحضور `Online`/`Offline`/`Away`، وHTTP API لعمليات المحادثات الدائمة بما يشمل القنوات والعضوية والأرشفة والبحث وحالة القراءة. لا يتجاوز أي Hub قواعد Application أو التخزين الدائم.

تنفذ هذه الخطوة حزم WBS الداخلية `3.3.1` إلى `3.3.4` على فرع واحد هو `feature/3.3-chat-presence`، لتبقى مراجعة خطوة الشات والحضور بوابة واحدة قبل 3.4.

## الحدود المعمارية

| الطبقة | المسؤولية المنفذة |
|---|---|
| `Domain` | الكيانات `ChatChannel` و`ChannelMember` و`ChatMessage` و`MessageReadState`، والأنواع `Direct`/`Group`/`Channel` و`Online`/`Offline`/`Away`، وقواعد الاسم وحجم الرسالة والحذف المنطقي. |
| `Application` | الأوامر والاستعلامات، Validators، وواجهات `IChatChannelRepository` و`IChatMessageRepository` و`IUnitOfWork` و`IUserDirectory` و`IPresenceTracker` و`IChatRealtimePublisher`. |
| `Infrastructure` | EF Core repositories، وحدة عمل EF، تكامل دليل ASP.NET Identity، متتبع حضور مؤقت، وناشر SignalR. |
| `API` | Controllers التي تحقن `IMediator` فقط، و`ChatHub` المحمي عند `/hubs/chat`، وتكوين JWT لتمرير access token أثناء اتصال Hub. |

تُنشأ الرسالة عبر `POST /api/v1/channels/{channelId}/messages` وتُحفظ أولًا من خلال Use Case و`IUnitOfWork`، ثم يبث `IChatRealtimePublisher` حدث `messageCreated` للمشتركين في القناة. وبذلك لا يعتمد حفظ الرسائل على SignalR، ولا ينشئ Hub رسالة مباشرة أو يتجاوز CQRS.

## HTTP API

| المسار | السلوك والصلاحية |
|---|---|
| `GET /api/v1/channels` | قنوات المستخدم الحالي. |
| `POST /api/v1/channels` | ينشئ قناة `Direct` أو `Group` أو `Channel` ويشترط وجود الأعضاء؛ القناة الفردية تضم عضوين متميزين فقط ويعاد استخدامها بدل تكرارها. |
| `GET /api/v1/channels/{channelId}` | يقرأ عضو القناة تفاصيلها. |
| `PATCH /api/v1/channels/{channelId}` | يحدّث الاسم أو `isArchived` للمالك أو `Admin`. لا تعاد تسمية القناة الفردية. |
| `POST` و`DELETE /api/v1/channels/{channelId}/members...` | يضيف أو يزيل عضوًا للمالك أو `Admin`، مع حماية المالك الأخير. |
| `GET` و`POST /api/v1/channels/{channelId}/messages` | بحث أساسي من خلال `search` و`take` من 1 إلى 100، وإرسال رسائل دائمة للأعضاء فقط. |
| `PATCH` و`DELETE /api/v1/messages/{messageId}` | تعديل المرسل للرسالة، أو حذف منطقي للمرسل أو `Admin`. لا تظهر الرسائل المحذوفة في البحث. |
| `POST /api/v1/messages/{messageId}/read` | تحديث آخر رسالة مقروءة للعضو مع بث `messageRead`. |
| `GET /api/v1/presence/{userId}` | قراءة آخر حالة حضور معروفة؛ تكون `Offline` إن لم توجد جلسة نشطة في المثيل الحالي. |

تعيد نقاط النهاية الناجحة والفاشلة غلاف `ApiResponse` الموحد، بما في ذلك رفض العضوية `403` والمورد غير الموجود `404` وفشل قواعد المجال `400`.

## SignalR وحالة الحضور

`ChatHub` محمي بـ`[Authorize]` ويقبل JWT من `access_token` في query string فقط عند المسار `/hubs/chat`. لا تُضاف المجموعة الخاصة بالقناة إلا بعد أن يرسل Hub استعلام `VerifyChannelMembershipQuery` إلى Application. يدعم Hub الطرق `SubscribeChannel(channelId)` و`UnsubscribeChannel(channelId)` و`StartTyping(channelId)` و`StopTyping(channelId)` و`SetPresence(status)`.

| حدث الخادم | المحتوى |
|---|---|
| `messageCreated` / `messageUpdated` / `messageDeleted` | نموذج رسالة القناة بعد نجاح Use Case الدائم. |
| `messageRead` | آخر قراءة للمستخدم داخل القناة. |
| `typingChanged` | `channelId` و`userId` و`isTyping`؛ حدث مؤقت لا يكتب في قاعدة البيانات. |
| `presenceChanged` | `userId` و`status` ووقت التغيير. |

تستخدم هذه الخطوة `InMemoryPresenceTracker` كحالة حضور عابرة داخل مثيل التطبيق. هذه ليست طبقة حضور موزعة ولا دائمة؛ قبل النشر في أكثر من مثيل يجب اعتماد مزود حضور موزع صراحةً في Infrastructure مع إبقاء `IPresenceTracker` في Application بلا تغيير.

## البيانات والترحيل

أضيف migration باسم `AddChatAndPresence` للجداول `ChatChannels` و`ChannelMembers` و`ChatMessages` و`MessageReadStates` مع الفهارس اللازمة للقناة والعضوية والرسائل والقراءة. لا يُخزن الحضور العابر في SQL Server؛ أما القناة والرسالة وحالة القراءة فتبقى دائمة وتستخدم repositories و`IUnitOfWork` فقط من Application.

## الاختبارات والتحقق

تغطي اختبارات API إنشاء قناة والتحقق من وجود العضو والبحث وتعديل الرسائل والأرشفة والحذف المنطقي وحالات القراءة. ويوجد اختبار SignalR حي يستخدم Long Polling فوق خادم التكامل ويتحقق من `presenceChanged` للحالات Online/Away/Offline و`typingChanged` و`messageCreated` بعد حفظ الرسالة. كما يتحقق اختبار تفاوض Hub من رفض الوصول غير الموثق وقبول Bearer token.

تضاف حزمة `Microsoft.AspNetCore.SignalR.Client` إلى مشروع اختبارات التكامل فقط لتوفير دليل اتصال Hub فعلي؛ لا تُنشر كاعتماد لتطبيق API.
