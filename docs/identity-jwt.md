# WBS 3.2 — Identity & JWT

## رقم WBS

**3.2 — Identity & JWT**

## المخرج المتوقع ومعيار القبول

> Secure registration and login endpoints, token refresh rotation, JWT authentication, seeded `Admin` / `Manager` / `Employee` roles, and protected sample endpoints. Mediator and FluentValidation are registered exclusively through `AddApplication()`.

## التنفيذ

تُعرّف Application أوامر `RegisterUserCommand` و`LoginUserCommand` و`RefreshAccessTokenCommand` ومعالجات Mediator وValidators الخاصة بها. يسجل `AddApplication()` كلاً من Mediator وFluentValidation وسلوك التحقق `ValidationBehavior`؛ ولا يسجل أي منها في API أو Infrastructure.

تحتوي Infrastructure على `ApplicationUser` وASP.NET Identity و`JwtTokenService` و`IdentityService`. تنشأ أدوار `Admin` و`Manager` و`Employee` عند الإقلاع عبر `IdentitySeeder`، مع توليد معرّف `Guid` صريح لكل دور. تسجل عملية التسجيل الموظف الجديد بدور `Employee`، بينما يصدر تسجيل الدخول زوجًا من Access Token وRefresh Token. تخزن Refresh Tokens بصيغة SHA-256 hash، وتنفذ عملية Refresh تدويرًا يلغي الرمز السابق قبل إصدار زوج جديد.

تستهلك API الأوامر فقط عبر `IMediator` وتفعل JWT Bearer Authentication. المسارات هي `POST /api/v1/auth/register` و`POST /api/v1/auth/login` و`POST /api/v1/auth/refresh` و`GET /api/v1/profile`. ويوجد `GET /api/v1/management/ping` كنموذج حماية بالأدوار `Admin` أو `Manager`. يعيد معالج التفويض في API غلاف الاستجابة الموحد لحالتي `401 Unauthorized` و`403 Forbidden` بدل جسم استجابة فارغ. ويعلن `/openapi/v1.json` الآن مخطط JWT Bearer ومتطلبات `security` للعمليات التي تحمل `[Authorize]`، مع استثناء نقاط `AuthController` العامة و`HealthController`؛ تفاصيل القرار المنجز موثقة في `docs/adr/ADR-005-openapi-jwt-security-documentation.md`.

## قرار الوسيط وترخيصه

استُبدلت حزمة MediatR نهائيًا بحزم `Mediator.Abstractions` و`Mediator.SourceGenerator`، الإصدار `3.0.2`، من مشروع [martinothamar/Mediator](https://github.com/martinothamar/Mediator). الترخيص الرسمي هو [MIT](https://github.com/martinothamar/Mediator/blob/main/LICENSE)، وهو ترخيص متساهل دائم يسمح بالاستخدام والنسخ والتعديل والتوزيع والبيع مع حفظ إشعار الحقوق والترخيص. تؤكد [بيانات NuGet الرسمية لـMediator.Abstractions 3.0.2](https://api.nuget.org/v3-flatcontainer/mediator.abstractions/3.0.2/mediator.abstractions.nuspec) الاعتمادات المباشرة للحزمة ولا تتضمن MediatR أو Lucky Penny. يوثق [ADR-003](adr/ADR-003-mediator-library.md) قرار الموضع والضوابط الدورية.

## إدارة الأسرار

توجد قيمة `Jwt:Key` في `appsettings.json` للتطوير المحلي فقط، وليست سرًا إنتاجيًا. قبل النشر يجب تقديم مفتاح قوي عبر User Secrets أو متغير بيئة `Jwt__Key` أو مخزن أسرار معتمد، وعدم تسجيل access/refresh tokens أو كلمات المرور في السجلات.

## الاعتماديات المضافة ولماذا

| الاعتمادية | الموضع | التبرير |
|---|---|---|
| `Mediator.Abstractions` 3.0.2 | Application وAPI | واجهات `IRequest` و`IRequestHandler` و`IMediator` المتوافقة مع نمط الوسيط، بترخيص MIT. |
| `Mediator.SourceGenerator` 3.0.2 | Application (PrivateAssets) | يولد تسجيل DI وتنفيذ `IMediator` في التجميع الذي يحتوي `AddApplication()`، ولا يضاف إلى API لتفادي تكرار الكود المولد. |
| `FluentValidation.DependencyInjectionExtensions` | Application | التحقق من مدخلات المصادقة عبر التسجيل في `AddApplication()`. |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Infrastructure | مخازن ASP.NET Identity وEntity Framework Core. |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | API | التحقق من JWT على حدود HTTP فقط. |
| `Microsoft.EntityFrameworkCore.InMemory` | Infrastructure وAPI Integration Tests | قاعدة بيانات ذاكرة للاختبارات فقط؛ تختارها Infrastructure عند بيئة `Testing` باسم فريد لكل مضيف، لذلك لا يجتمع موفر PostgreSQL وموفر InMemory في حاوية DI واحدة. |
| `Microsoft.Extensions.Hosting.Abstractions` | Infrastructure | قراءة بيئة الاستضافة لاختيار موفر بيانات الاختبار دون ربط Application أو API بقاعدة الاختبار. |
| `Microsoft.EntityFrameworkCore.Design` | API (PrivateAssets) | تمكين فحص وتوليد migrations عبر مشروع بدء التشغيل، ولا يُنشر كاعتماد تشغيلي. |

## معايير تحقق التنفيذ

يجب أن يمر البناء، وأن تغطي اختبارات API التسجيل والدخول وتجديد الرمز ونقطة الملف الشخصي وحالة كلمة المرور غير المطابقة، وحالتي الرفض `401` و`403`، وأن يبقى كل API response الناجح أو المرفوض ضمن الغلاف الموحد. تعمل اختبارات التكامل في بيئة `Testing` فقط، فتستخدم `EnsureCreatedAsync` بدلًا من migrations ولا تتصل بـPostgreSQL المحلي.
