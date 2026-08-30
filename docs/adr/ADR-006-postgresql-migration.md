# ADR-006 — ترحيل مزود قاعدة البيانات إلى PostgreSQL

| الحقل | القيمة |
|---|---|
| الحالة | **مقترح** في WBS 3.7؛ يصبح **مقبولًا** بعد اعتماد ودمج طلب السحب |
| التاريخ | 2026-08-30 |
| القرار | استبدال SQL Server بـ PostgreSQL عبر `Npgsql.EntityFrameworkCore.PostgreSQL`، وإعادة توليد هجرة أولية واحدة متوافقة مع PostgreSQL |

## السياق

كان الدليل المعماري الأصلي يعتمد SQL Server مع Entity Framework Core Code-First. الخطوة WBS 3.7 تُحضّر استضافة الـAPI على Render. Render يوفّر PostgreSQL مُدارًا كخدمة أصلية، بينما استضافة SQL Server تتطلب مسارًا منفصلًا أكثر تعقيدًا. لا توجد بيانات إنتاجية يجب الحفاظ عليها في سلسلة هجرات SQL Server الحالية.

## القرار

يُعتمد ما يأتي في WBS 3.7 وما بعدها:

| الموقع | التغيير | السبب |
|---|---|---|
| `UltimateSolution.Infrastructure` | استبدال `Microsoft.EntityFrameworkCore.SqlServer` بـ `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 | مزود EF Core الرسمي لـ PostgreSQL، متوافق مع .NET 10 / EF Core 10. |
| `UltimateSolution.Infrastructure` | إضافة صريحة لـ `System.IdentityModel.Tokens.Jwt` 8.19.2 | كان `JwtTokenService` يعتمد على هذه الأنواع عبر سلسلة انتقالية من `Microsoft.Data.SqlClient`. بعد إزالة SQL Server لزم المرجع المباشر، وهو الإصدار نفسه الذي يسحبه `JwtBearer` 10.0.11 في API. |
| `AddInfrastructure` و`ApplicationDbContextFactory` | `UseNpgsql` بدل `UseSqlServer` | حصر معرفة المزود في Infrastructure فقط، كما يفرض اتجاه الاعتماد. |
| `Persistence/Migrations` | حذف هجرات SQL Server وإعادة توليد `InitialPostgreSql` | الهجرات السابقة تحتوي أنواعًا وتعليقات خاصة بـ SQL Server مثل `nvarchar` و`uniqueidentifier` و`SqlServer:Identity`. |
| جذر المستودع | `render.yaml` و`Dockerfile` | وصف خدمة Web وقاعدة PostgreSQL المطلوبتين على Render، دون نشر في هذه الخطوة. |

تبقى الاختبارات في بيئة `Testing` على `UseInMemoryDatabase`. لا يتصل Application أو API أو Domain بمزود قاعدة بيانات بالاسم.

## نتائج مراجعة الهجرات السابقة

| العنصر الخاص بـ SQL Server | أين ظهر | المعالجة |
|---|---|---|
| `nvarchar` و`uniqueidentifier` و`datetime2` | ملفات الهجرة و`ApplicationDbContextModelSnapshot` | أُزيلت مع إعادة التوليد. PostgreSQL يستخدم `character varying` و`uuid` و`timestamp with time zone` لأن الكيانات تعتمد `DateTimeOffset`. |
| `SqlServerModelBuilderExtensions.UseIdentityColumns` | Designer وSnapshot | أُزيل. جداول Identity التي تستخدم `int` تعتمد تسلسل/هوية PostgreSQL التي يولدها Npgsql. |
| `Annotation("SqlServer:Identity", "1, 1")` | `AddIdentityAndRefreshTokens` | أُزيل مع سلسلة الهجرات القديمة. |
| دوال مثل `GETDATE()` أو `NEWID()` | غير موجودة | لا يلزم استبدال دالة محرك. |

لا توجد بيانات تشغيلية لنقلها. إعادة الهجرة الأولية آمنة لأن البيئة لم تُنشر بعد.

## غير مشمول في هذه الخطوة

| البند | الحالة |
|---|---|
| نشر فعلي على Render | مؤجل. الملف `render.yaml` توثيق Blueprint فقط، و`autoDeployTrigger` مضبوط على `off`. |
| خطة Render التجارية والمنطقة | تُختار عند خطوة النشر. |
| مفاتيح JWT وأسرار الوسائط والذكاء الاصطناعي | تبقى متغيرات بيئة/`sync: false`، ولا تُكتب في المستودع. |

## النتائج والضوابط

- الدليل المعماري يعتمد PostgreSQL + EF Core بدل SQL Server.
- أي هجرة لاحقة تُولَّد بـ `UseNpgsql` فقط.
- قبل اعتماد أي تحديث لـ Npgsql، يُشغَّل `dotnet list UltimateSolution.Communication.slnx package --include-transitive` للتحقق من غياب `MediatR` و`LuckyPenny`، ومن غياب `Microsoft.EntityFrameworkCore.SqlServer`.
