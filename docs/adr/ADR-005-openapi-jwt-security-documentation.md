# ADR-005: توثيق حماية JWT Bearer في OpenAPI

- **الحالة:** مقبول ومنجز
- **التاريخ:** 2026-08-28
- **النطاق:** Backend API / OpenAPI

## السياق

كان التطبيق يفرض JWT Bearer فعليًا عبر `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` و`AddJwtBearer`، لكن وثيقة OpenAPI المنشورة لا تحتوي على `components.securitySchemes` ولا متطلبات `security` لكل عملية. أدى ذلك إلى انفصال بين الحماية الفعلية وتجربة Swagger/OpenAPI، بحيث لا يستطيع المستهلك معرفة أن العمليات المحمية تتطلب Bearer token من العقد المنشور.

## القرار

يستخدم التطبيق محولات `Microsoft.AspNetCore.OpenApi` في .NET 10 لإضافة مخطط أمان HTTP Bearer باسم `Bearer` إلى وثيقة OpenAPI، وإضافة متطلب أمان لكل عملية لا تحمل `AllowAnonymousAttribute`. تبقى عمليات المصادقة العامة والصحة بلا متطلب Bearer وفق metadata الفعلية.

يُنفذ ذلك في `BearerSecuritySchemeTransformer` عبر `IOpenApiDocumentTransformer` للمخطط العام و`IOpenApiOperationTransformer` للمتطلبات الشرطية. لا يغيّر القرار قواعد التفويض في runtime ولا يمنح صلاحية جديدة؛ هو توثيق آلي للحماية القائمة.

## حالة التنفيذ

تم تنفيذ القرار على فرع `feature/3.6-openapi-security` والتحقق من نجاح `dotnet build`. بعد الدمج، يجب إعادة توليد `/openapi/v1.json` والتحقق من وجود:

- `components.securitySchemes.Bearer` من النوع HTTP وبـ`scheme: bearer`.
- `security` للعمليات المحمية.
- غياب `security` عن عمليات `AuthController` العامة و`HealthController`.

وبذلك أُزيل بند «إضافة JWT Bearer إلى توليد OpenAPI» من قائمة القرارات المفتوحة قبل الإنتاج، بينما تبقى قرارات **مفتاح JWT الإنتاجي** وIssuer/Audience والتدوير وإدارة الأسرار مفتوحة لأنها تشغيلية وليست جزءًا من توثيق العقد.

## البدائل المرفوضة

تم رفض إضافة متطلب أمان عالمي بلا استثناء، لأنه سيصف عمليات login/register/refresh وhealth بصورة خاطئة. كما تم رفض الاعتماد على توثيق يدوي منفصل، لأن العقد الحي يجب أن يعكس metadata الفعلية للتطبيق.
