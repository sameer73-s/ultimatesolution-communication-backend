# WBS 3.1 — Backend Foundation

## رقم WBS

**3.1 — Backend Foundation**

## المخرج المتوقع ومعيار القبول

> EF Core Code-First context and reviewed initial migration; unified `success` / `data` / `message` / `errors` responses and middleware tests; the Domain, Application, Infrastructure, API, and test projects compile.

## ما تم إنشاؤه

يتضمن الحل طبقات `Domain` و`Application` و`Infrastructure` و`API` ومشروعي اختبارات. يحافظ اتجاه الاعتماد على أن API تعتمد على Application وInfrastructure، وأن Infrastructure تعتمد على Application وDomain، وأن Application تعتمد على Domain فقط.

يحتوي Application على `IApplicationDbContext` كعقد قاعدة البيانات. لا تستخدم Application أو Controllers أو Hubs `ApplicationDbContext` مباشرة. يسجل Infrastructure التنفيذ المستند إلى EF Core وSQL Server داخل `AddInfrastructure`. ويقيم `ApplicationDbContextFactory` داخل Infrastructure لإنشاء Migrations وقت التصميم، فلا يحتاج مشروع API إلى اعتماد أدوات EF Core الخاصة بالتصميم.

تضمن `ExceptionHandlingMiddleware` غلاف أخطاء موحدًا، ويثبت `HealthController` شكل الاستجابة الناجحة. يمتلك المشروع نقطة OpenAPI على `/openapi/v1.json` لتكون لاحقًا مصدر العقد الحي لعميل Flutter.

## ملاحظة القرارات المعمارية

لم تضف هذه الخطوة أي استخدام مباشر لـ Jitsi أو WebRTC أو موفد ذكاء اصطناعي أو MailKit/Microsoft Graph. تبقى هذه التكاملات مؤجلة للخطوات المطابقة، وتلتزم بمسارات التجريد الموثقة في ADR-001 وADR-002.

## الاعتماديات المضافة ولماذا

| الاعتمادية | الموضع | التبرير |
|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | Infrastructure | مزود SQL Server المعتمد لإنشاء DbContext وMigrations بنهج Code-First. |
| `Microsoft.EntityFrameworkCore.Design` | Infrastructure | أدوات التصميم ومولد EF Core Migrations فقط؛ لا تنشر مع Runtime. |
| `Microsoft.AspNetCore.OpenApi` | API | إخراج العقد الحي OpenAPI المعتمد لمستهلكي API. |
| `Microsoft.AspNetCore.Mvc.Testing` | API Integration Tests | اختبار HTTP لتغليف الاستجابات في التطبيق المستضاف. |
| `Microsoft.Extensions.DependencyInjection` | Application Tests | اختبار نقطة تسجيل Application باستخدام `ServiceCollection` في نطاق الأساس فقط. |

## التشغيل المحلي

عيّن `ConnectionStrings__DefaultConnection` في بيئتك وفق `.env.example`، ثم نفذ:

```bash
dotnet build UltimateSolution.Communication.slnx
dotnet test UltimateSolution.Communication.slnx
dotnet run --project src/UltimateSolution.API
```

لا توجد قاعدة بيانات مطلوبة لتشغيل Health endpoint أو الاختبارات الحالية. يتطلب تطبيق migrations اتصال SQL Server محليًا صحيحًا.
