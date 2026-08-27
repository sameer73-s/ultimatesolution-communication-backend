# WBS 3.5 — AI Meeting Intelligence

تضيف هذه الخطوة نموذجًا كاملاً لتفريغ التسجيلات وملخصات الاجتماعات والمهام المستخرجة، مع إبقاء الذكاء الاصطناعي خدمة خارجية عن الـAPI الرئيسي. لا يتصل أي Controller أو Handler بمزوّد تفريغ أو تلخيص مباشرة؛ تعتمد Application على `ITranscriptionService` و`ISummaryService` فقط، وتوجد التفاصيل القابلة للتهيئة داخل `Infrastructure/ExternalServices/Ai`.

## تدفق البيانات والحوكمة

| المرحلة | الكيان أو العقد | الضابط |
|---|---|---|
| طلب التفريغ | `RequestTranscriptionCommand` و`TranscriptionJob` | لا يطلبه إلا منظم الاجتماع أو Manager؛ ولا يقبل تسجيلًا ما زال في حالة `Recording`. |
| نتيجة التفريغ | `TranscriptionSegment` | ترتب المقاطع بفهرس فريد `(TranscriptionJobId, SequenceNumber)` وتحفظ الحالة صراحة. |
| توليد الملخص | `GenerateMeetingSummaryCommand` و`ISummaryService` | لا يبدأ إلا من تفريغ مكتمل، وينشئ دائمًا `MeetingSummary` بحالة `Draft`. |
| المراجعة والاعتماد | `ApproveMeetingSummaryCommand` و`IMeetingSummaryApprovalPolicy` | لا يفحص الـHandler دور المنظم أو Manager مباشرة؛ تستدعي Application السياسة المجردة التي ينفذها Infrastructure. |
| تحويل المهام | `ActionItem` | لا تحفظ المهمة الدائمة إلا بعد نجاح السياسة واعتماد الملخص. يقتصر أي مسؤول مقترح من الاختبار على مشارك في الاجتماع. |

> **قاعدة ADR-002:** يمكن إضافة مراجع أو قاعدة تفويض مستقبلية بتعديل تنفيذ `IMeetingSummaryApprovalPolicy` فقط، من دون تغيير أمر الاعتماد أو الكيان أو Controller.

## المحولات والاختبار

يسجل التشغيل الاعتيادي `ExternalMeetingIntelligenceService`، وهو محول HTTP عام إلى خدمة ذكاء اجتماعات مستقلة. إعداداته العامة هي `MeetingIntelligence:ServiceUrl` و`MeetingIntelligence:ApiKey`، ولا تتضمن اسم Whisper أو Claude أو Azure أو أي SDK خاص بمزوّد. عند غياب العنوان، يفشل المحول برمز نتيجة معروف ولا يحاول إجراء معالجة محلية داخل الـAPI الرئيسي.

تستخدم بيئة `Testing` بدلًا من ذلك `TestMeetingIntelligenceService`. ينتج هذا المحول تفريغًا وملخصًا وقرارات ومهمة مقترحة حتمية، كي يختبر `MeetingIntelligenceEndpointsTests` دورة التفريغ، المسودة، رفض المشارك غير المخول، اعتماد Manager عبر السياسة، إنشاء المهمة بعد الاعتماد فقط، وتحديثها من المسؤول.

## حدود متعمدة وخطوة لاحقة

لا تربط هذه الخطوة Whisper أو Claude ربطًا فعليًا، ولا تضيف طابور خلفية أو تخزين ملفات أو سياسة احتفاظ أو أسرار تشغيلية. وثّق [ADR-004](adr/ADR-004-ai-meeting-intelligence-integration-boundary.md) أن هذه عناصر خطوة منفصلة قبل الإنتاج. صُممت العقود لتسمح بربط Whisper مفتوح المصدر مُستضاف ذاتيًا خلف `ITranscriptionService` لاحقًا، من دون افتراض API لمزوّد سحابي مُدار.

## واجهات HTTP

| الطريقة والمسار | النتيجة |
|---|---|
| `POST /api/v1/recordings/{recordingId}/transcription` | يعيد `202 Accepted` مع حالة Job؛ في بيئة الاختبار تكتمل النتيجة حتميًا. |
| `GET /api/v1/meetings/{meetingId}/transcription` | يعرض آخر Job ومقاطع التفريغ للمشارك. |
| `POST /api/v1/meetings/{meetingId}/summary/generate` | يعيد `202 Accepted` مع `MeetingSummary` في `Draft`. |
| `GET /api/v1/meetings/{meetingId}/summary` | يعرض آخر ملخص للمشارك. |
| `POST /api/v1/meetings/{meetingId}/summary/approve` | يعتمد الملخص عبر السياسة ويُنشئ Action Items المؤكدة. |
| `GET /api/v1/action-items` | يعرض المهام المعينة للمستخدم الحالي. |
| `PATCH /api/v1/action-items/{actionItemId}` | يحدث المهمة بواسطة مسؤولها أو Manager. |
