# Ultimate Solution Communication — Backend

هذا المستودع مخصص لحل **ASP.NET Core** الخاص بمنصة التواصل المؤسسي. يُبنى الحل وفق Clean Architecture: Domain وApplication وInfrastructure وAPI، مع CQRS/Mediator وRepository + Unit of Work وPostgreSQL/EF Core.

## حالة المستودع

لا يُدفع مباشرة إلى `main`؛ كل خطوة WBS تُسلَّم عبر Pull Request. الخطوة الحالية هي `feature/3.7-postgresql-migration` وفق حزمة WBS `3.7`. مزود البيانات التشغيلي هو PostgreSQL عبر Npgsql، كما يوثق [ADR-006](docs/adr/ADR-006-postgresql-migration.md).

## قواعد غير قابلة للتجاوز

- لا يُدفع مباشرة إلى `main`؛ راجع [CONTRIBUTING.md](CONTRIBUTING.md).
- كل Use Case يمر عبر Command أو Query ومعالجة Application، ولا يستدعي Controller أو Hub قاعدة البيانات مباشرة.
- Jitsi معزول داخل Infrastructure فقط خلف `IMeetingMediaService` في Application.
- تفويض اعتماد ملخص AI يمر عبر سياسة Application قابلة للتوسعة، لا فحص دور ثابت داخل Handler.
- Swagger/OpenAPI هو مصدر عقد التكامل الحي مع Flutter.

## المرجع الحي

يشير هذا المستودع إلى [مستودع وثائق المشروع](https://github.com/sameer73-s/ultimatesolution-communication-docs) للدليل المعماري وADRs ومخططات المرحلة 1.
