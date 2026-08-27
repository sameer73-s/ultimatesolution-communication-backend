# WBS 3.2 — Identity & JWT

## رقم WBS

**3.2 — Identity & JWT**

## المخرج المتوقع ومعيار القبول

> Secure registration and login endpoints, token refresh rotation, JWT authentication, seeded `Admin` / `Manager` / `Employee` roles, and protected sample endpoints. MediatR and FluentValidation are registered exclusively through `AddApplication()`.

## التنفيذ

تُعرّف Application أوامر `RegisterUserCommand` و`LoginUserCommand` و`RefreshAccessTokenCommand` ومعالجات MediatR وValidators الخاصة بها. يسجل `AddApplication()` كلاً من MediatR وFluentValidation وسلوك التحقق `ValidationBehavior`؛ ولا يسجل أي منها في API أو Infrastructure.

تحتوي Infrastructure على `ApplicationUser` وASP.NET Identity و`JwtTokenService` و`IdentityService`. تنشأ أدوار `Admin` و`Manager` و`Employee` عند الإقلاع عبر `IdentitySeeder`، مع توليد معرّف `Guid` صريح لكل دور. تسجل عملية التسجيل الموظف الجديد بدور `Employee`، بينما يصدر تسجيل الدخول زوجًا من Access Token وRefresh Token. تخزن Refresh Tokens بصيغة SHA-256 hash، وتنفذ عملية Refresh تدويرًا يلغي الرمز السابق قبل إصدار زوج جديد.

تستهلك API الأوامر فقط عبر `ISender` وتفعل JWT Bearer Authentication. المسارات هي `POST /api/v1/auth/register` و`POST /api/v1/auth/login` و`POST /api/v1/auth/refresh` و`GET /api/v1/profile`. ويوجد `GET /api/v1/management/ping` كنموذج حماية بالأدوار `Admin` أو `Manager`. يعيد معالج التفويض في API غلاف الاستجابة الموحد لحالتي `401 Unauthorized` و`403 Forbidden` بدل جسم استجابة فارغ.

## إدارة الأسرار

توجد قيمة `Jwt:Key` في `appsettings.json` للتطوير المحلي فقط، وليست سرًا إنتاجيًا. قبل النشر يجب تقديم مفتاح قوي عبر User Secrets أو متغير بيئة `Jwt__Key` أو مخزن أسرار معتمد، وعدم تسجيل access/refresh tokens أو كلمات المرور في السجلات.

## الاعتماديات المضافة ولماذا

| الاعتمادية | الموضع | التبرير |
|---|---|---|
| `MediatR` | Application | تطبيق CQRS ومعالجة أوامر المصادقة. |
| `FluentValidation.DependencyInjectionExtensions` | Application | التحقق من مدخلات المصادقة عبر التسجيل في `AddApplication()`. |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Infrastructure | مخازن ASP.NET Identity وEntity Framework Core. |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | API | التحقق من JWT على حدود HTTP فقط. |
| `Microsoft.EntityFrameworkCore.InMemory` | Infrastructure وAPI Integration Tests | قاعدة بيانات ذاكرة للاختبارات فقط؛ تختارها Infrastructure عند بيئة `Testing` باسم فريد لكل مضيف، لذلك لا يجتمع موفر SQL Server وموفر InMemory في حاوية DI واحدة. |
| `Microsoft.Extensions.Hosting.Abstractions` | Infrastructure | قراءة بيئة الاستضافة لاختيار موفر بيانات الاختبار دون ربط Application أو API بقاعدة الاختبار. |
| `Microsoft.EntityFrameworkCore.Design` | API (PrivateAssets) | تمكين فحص وتوليد migrations عبر مشروع بدء التشغيل، ولا يُنشر كاعتماد تشغيلي. |

## معايير تحقق التنفيذ

يجب أن يمر البناء، وأن تغطي اختبارات API التسجيل والدخول وتجديد الرمز ونقطة الملف الشخصي وحالة كلمة المرور غير المطابقة، وحالتي الرفض `401` و`403`، وأن يبقى كل API response الناجح أو المرفوض ضمن الغلاف الموحد. تعمل اختبارات التكامل في بيئة `Testing` فقط، فتستخدم `EnsureCreatedAsync` بدلًا من migrations ولا تتصل بـSQL Server المحلي.
