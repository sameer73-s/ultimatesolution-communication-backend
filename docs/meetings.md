# WBS 3.4 — Meetings

## نطاق الخطوة ومعيار القبول

> **المخرج المتوقع:** حالات استخدام الجدولة والمشاركين والأجندة، وواجهة `IMeetingMediaService` العامة، وتنفيذ `JitsiMeetingMediaService` داخل Infrastructure فقط، ثم تدفقات البدء والانضمام والمغادرة والإنهاء والتسجيل عبر Commands. يجب أن تستدعي Controllers الأوامر وأن يستدعي كل Handler `IMeetingMediaService` فقط، من دون اعتماد مباشر على Jitsi خارج Infrastructure.

تشمل هذه الخطوة حزم WBS `3.4.1` إلى `3.4.4` وتلتزم بقرار ADR-001 المعتمد.

## العزل المعماري الإلزامي

| الطبقة | المكونات المنفذة | ما لا تعرفه |
|---|---|---|
| `Domain` | `Meeting`، و`MeetingParticipant`، و`MeetingRecording`، وحالات الاجتماع والتسجيل وقواعد انتقال الحالة. | Jitsi وWebRTC وعناوين الغرف والتواقيع وURLs الخاصة بالمزوّد. |
| `Application` | أوامر/استعلامات وValidators و`IMeetingRepository` و`IMeetingMediaService` وعقود الوسائط العامة. | أي SDK أو API أو إعداد أو DTO يحمل اسم Jitsi. |
| `Infrastructure` | مستودع EF Core و`JitsiMeetingMediaService` و`JitsiMeetingMediaOptions`. | تفويض الأعمال النهائي وقواعد دورة الاجتماع. |
| `API` | `MeetingsController`، وJWT، وSwagger/OpenAPI، ونماذج طلب HTTP المحايدة. | إنشاء أو استدعاء Jitsi مباشرة أو معرفة اسم تنفيذ المحول. |

> لا يظهر اسم `Jitsi` في Domain أو Application أو API أو عقود HTTP أو أسماء حقول JSON. الربط الوحيد بين الواجهة والتنفيذ يتم داخل `AddInfrastructure` بواسطة `IMeetingMediaService -> JitsiMeetingMediaService`.

## نموذج الاجتماع وقواعد الحالة

`IMeetingMediaService` يعيد `Result` أو `Result<T>` لكل عملية؛ يفحص Handler `IsSuccess` وقيمة النتيجة قبل أي انتقال حالة أو حفظ. لذلك لا يصبح الاجتماع `Active` قبل نجاح البدء، ولا يصبح `Completed` قبل نجاح الإنهاء، ولا ينشأ كيان `MeetingRecording` قبل نجاح بدء التسجيل. يبقى كود خطأ المزوّد داخل Infrastructure، ويُعاد فشل الوسائط للـAPI كفشل مجال عام لا يكشف تفاصيل المزوّد.

`Meeting` يبدأ بحالة `Scheduled` وينتقل إلى `Active` بعد نجاح `StartMeetingAsync` وإرجاع `MediaSessionReference` عام غير فارغ. ثم ينتقل إلى `Completed` فقط بعد نجاح `EndMeetingAsync`. لا يمكن تعديل الموعد أو الأجندة أو المشاركين بعد بدء الاجتماع، ولا يمكن بدء تسجيل إلا لاجتماع `Active`.

ينشئ التنظيم تلقائيًا مشاركًا بدور `Organizer`، ولا يسمح بإزالته. يمكن للمنظم أو المستخدم ذي دور `Manager` أو `Admin` تعديل الاجتماع، وإدارة المشاركين، وتشغيل الاجتماع وإنهاؤه، والتحكم بالتسجيل. أما التفاصيل والانضمام والمغادرة وقائمة التسجيلات فتتطلب عضوية الاجتماع.

| الحالة | عمليات مسموحة | عمليات مرفوضة |
|---|---|---|
| `Scheduled` | تعديل، دعوة/إزالة حاضر، بدء. | انضمام أو تسجيل أو إنهاء. |
| `Active` | انضمام، مغادرة، بدء/إيقاف التسجيل، إنهاء. | تعديل الوقت/الأجندة أو تغيير المشاركين أو بدء ثانٍ. |
| `Completed` | قراءة التفاصيل والتسجيلات للمشاركين. | تعديل، بدء، انضمام، تسجيل أو إنهاء ثانٍ. |

## HTTP API وعقود الوسائط

| Endpoint | Command/Query | التفويض |
|---|---|---|
| `GET/POST /api/v1/meetings` | `GetMeetingsQuery` / `ScheduleMeetingCommand` | مستخدم موثق. |
| `GET/PATCH /api/v1/meetings/{meetingId}` | قراءة/تعديل الاجتماع | عضو للقراءة؛ منظم أو Manager/Admin للتعديل. |
| `POST` و`DELETE /api/v1/meetings/{meetingId}/participants...` | دعوة/إزالة مشارك | منظم أو Manager/Admin قبل البدء. |
| `POST /start` و`/end` و`/join` و`/leave` | تدفقات الوسائط | حسب دور المنظم أو عضوية المشارك. |
| `POST /recording/start` و`/recording/stop` | تدفقات التسجيل | منظم أو Manager/Admin لاجتماع نشط. |
| `GET /recordings` | `GetMeetingRecordingsQuery` | مشارك موثق. |

لا يُعرض للعميل سوى `mediaSessionReference` و`mediaJoinUrl` و`expiresAtUtc` بوصفها عقودًا عامة. لا يحتوي أي Command أو DTO أو Controller على غرفة أو رمز أو نطاق أو اسم مزوّد محدد.

## تنفيذ Infrastructure للوسائط

`JitsiMeetingMediaService` هو المحول الوحيد الخاص بالمزوّد، في `Infrastructure/ExternalServices/Meetings`. ينشئ مرجع جلسة معتمًا، وينشئ عند الانضمام JWT موقّعًا وإسقاطه إلى `MediaJoinUrl` عام. تبقى مادة إعداد التوقيع وكافة تفاصيل تكوين المزوّد في Infrastructure فقط، تحت القسم العام `MeetingMedia`.

قيم `appsettings.json` الحالية تطويرية فقط، تمامًا كإعداد JWT المحلي. قبل الإنتاج يجب تمرير `MeetingMedia__BaseUrl` و`MeetingMedia__AppId` و`MeetingMedia__ApiSecret` من مخزن أسرار، وتكوين بيئة Jitsi الآمنة وسياسة التسجيل. لا يجب وضع السر أو اسم المحول في Flutter أو API أو Application.

## قيد تشغيلي حرج — استمرارية جلسات وتسجيلات الوسائط

> **الحالة الحالية ليست مستمرة:** يحتفظ `JitsiMeetingMediaService` بحالة الجلسة والتسجيل داخل قاموسي `_sessions` و`_recordings` في ذاكرة عملية التطبيق فقط. هذه الآلية مقصودة لتدفقات التطوير والاختبارات في مثيل واحد، وليست تخزينًا تشغيليًا دائمًا أو موزعًا.

يبقى صف `Meeting` في قاعدة البيانات بحالة `Active` ويحفظ `MediaSessionReference`، كما تبقى صفوف `MeetingRecording` محفوظة. لكن إعادة تشغيل التطبيق تمحو القاموسين المؤقتين. ونتيجة لذلك، لا يستطيع المحول الحالي استئناف العثور على جلسة الوسائط النشطة؛ فتعيد عمليات `Join` و`Leave` وبدء/إيقاف التسجيل و`End` فشل وسائط عام. تحمي بوابة `Result` في Application البيانات من انتقال حالة خاطئ، ولذلك لا تتحول قاعدة البيانات تلقائيًا إلى `Completed`، لكن الاجتماع يبقى ظاهرًا بصورة مضللة كـ`Active` إلى أن تتخذ معالجة تشغيلية.

| السيناريو بعد إعادة التشغيل | بيانات قاعدة البيانات | نتيجة المحول الحالي |
|---|---|---|
| `Join` لاجتماع `Active` | الاجتماع والـ`MediaSessionReference` ما زالا محفوظين. | يفقد القاموس الجلسة ويُرفض الانضمام. |
| `Start/Stop Recording` | الاجتماع والتسجيلات السابقة محفوظة. | يفقد القاموس حالة الجلسة أو التسجيل ويُرفض التحكم بالتسجيل. |
| `End` | الاجتماع يبقى `Active`. | يفقد القاموس الجلسة ولا ينهي Handler الاجتماع في قاعدة البيانات. |

**المعالجة إلزامية قبل الإنتاج:** لأن اسم الغرفة يشتق حتميًا بالفعل من `MeetingId` عبر `BuildRoomName(MeetingId)`، يجب أن يعيد محول Infrastructure بناء `RoomName` من `MeetingId` و`MediaSessionReference` المحفوظ بدل الاعتماد فقط على `_sessions`. كما يجب أن تصبح حالة التسجيل قابلة للاستعلام والتحكم من مزوّد الوسائط أو مخزن حالة موزع ودائم؛ لا يكفي قاموس ذاكرة محلي. يشمل ذلك سلوكًا آمنًا بعد إعادة التشغيل ومراجعة حقيقة حالة الغرفة أو التسجيل قبل تعديل الحالة الدائمة للاجتماع.

عند اعتماد تشغيل متعدد المثيلات أو مزود وسائط كامل، تُستبدل آلية إعادة البناء والحالة داخل Infrastructure فقط. تبقى واجهة `IMeetingMediaService` وجميع Use Cases وواجهات API وFlutter دون تعديل.

## البيانات

migration `AddMeetingsAndMedia` تضيف `Meetings` و`MeetingParticipants` و`MeetingRecordings` مع فهارس الجدولة والحالة والمشارك والتسجيل، وعلاقات خارجية مقيدة بمستخدمي ASP.NET Identity. تستخدم Application `IMeetingRepository` و`IUnitOfWork`؛ لا تصل إلى `ApplicationDbContext` مباشرة.

## التحقق

`MeetingsEndpointsTests.MeetingLifecycleUsesAuthorizedProviderNeutralMediaContracts` ينفذ تسلسلًا كاملاً: جدولة منظم وحاضر، رفض تعديل الحاضر، البدء، الانضمام بنتيجة عامة، بدء وإيقاف التسجيل، قراءة التسجيلات، المغادرة، الإنهاء، ورفض تعديل اجتماع مكتمل. يغطي الاختبار أيضًا اتصال Handlers بالمحول الحقيقي المسجل في Infrastructure بدلاً من mock.

كما يتحقق `ApplicationServiceCollectionExtensionsTests.AddApplicationRegistersMediatorHandlersAsScoped` من أن معالجات Mediator مسجلة بـ`Scoped` من داخل `AddApplication()`. يمنع ذلك احتجاز خدمات EF Core بين الطلبات، مع الإبقاء على التسجيل المعماري في المكان الإلزامي نفسه.
