# WBS 3.7 — Database Provider Migration

## رقم WBS

**3.7 — Database Provider Migration**

## المخرج المتوقع ومعيار القبول

> Backend persistence uses PostgreSQL through `Npgsql.EntityFrameworkCore.PostgreSQL`; SQL Server package and SQL Server-specific migrations are removed; a single PostgreSQL initial migration is regenerated; Application and Integration tests pass; `render.yaml` describes the Render Web Service and PostgreSQL database without deploying; ADR-006 records the hosting-driven provider change.

## ما تم إنجازه

استبدل مشروع Infrastructure حزمة `Microsoft.EntityFrameworkCore.SqlServer` بـ `Npgsql.EntityFrameworkCore.PostgreSQL` الإصدار `10.0.3`. يسجّل `AddInfrastructure` المزود عبر `UseNpgsql` عندما لا تكون البيئة `Testing` أو `Persistence:UseInMemory`. يبقى `ApplicationDbContextFactory` داخل Infrastructure لتوليد الهجرات وقت التصميم.

راجعت سلسلة الهجرات السابقة (`InitialCreate` حتى `AddNotifications`) ووجدت أنواعًا وتعليقات خاصة بـ SQL Server: `nvarchar` و`uniqueidentifier` و`datetime2` و`SqlServer:Identity`. لم تُستخدم دوال محرك مثل `GETDATE()` أو `NEWID()`. حُذفت السلسلة القديمة وأُعيد توليد هجرة أولية واحدة باسم `InitialPostgreSql` من النموذج الحالي.

أُضيف `render.yaml` لوصف خدمة Web تعمل من `Dockerfile` وقاعدة `ultimatesolution-communication-db` من نوع PostgreSQL 16، مع ربط `ConnectionStrings__DefaultConnection` من خاصية `connectionString`. النشر الفعلي على Render خارج نطاق هذه الخطوة، و`autoDeployTrigger` مضبوط على `off`.

## ملاحظة القرارات المعمارية

يوثق [ADR-006](adr/ADR-006-postgresql-migration.md) سبب الاستبدال: تبسيط الاستضافة على Render. لم يُضف أي اعتماد مباشر على Jitsi خارج Infrastructure، ولم تُضف MediatR أو أي حزمة من Lucky Penny.

## الاعتماديات المضافة أو المستبدلة ولماذا

| الاعتمادية | الموضع | التبرير |
|---|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 | Infrastructure | مزود EF Core لـ PostgreSQL المتوافق مع .NET 10. يستبدل `Microsoft.EntityFrameworkCore.SqlServer`. |
| `System.IdentityModel.Tokens.Jwt` 8.19.2 | Infrastructure | مرجع صريح لأنواع إصدار JWT. كانت تصل سابقًا بشكل انتقالي عبر `Microsoft.Data.SqlClient` بعد حزمة SQL Server. الإصدار يطابق اعتماد `JwtBearer` 10.0.11. |
| `dotnet-ef` 10.0.11 (local tool) | `.config/dotnet-tools.json` | أداة توليد الهجرات محليًا بعد إزالة سلسلة SQL Server. ليست اعتماد تشغيل. |

## التشغيل المحلي

عيّن `ConnectionStrings__DefaultConnection` وفق `.env.example` إلى PostgreSQL محلي، ثم نفّذ:

```bash
dotnet build UltimateSolution.Communication.slnx
dotnet test UltimateSolution.Communication.slnx
dotnet ef database update --project src/UltimateSolution.Infrastructure --startup-project src/UltimateSolution.API
dotnet run --project src/UltimateSolution.API
```

اختبارات الوحدة والتكامل لا تتطلب PostgreSQL؛ فهي تستخدم قاعدة InMemory في بيئة `Testing`.
