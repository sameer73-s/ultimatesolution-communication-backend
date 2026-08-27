# Ultimate Solution Communication — Backend

هذا المستودع مخصص لحل **ASP.NET Core** الخاص بمنصة التواصل المؤسسي. سيبنى الحل وفق Clean Architecture: Domain وApplication وInfrastructure وAPI، مع CQRS/MediatR وRepository + Unit of Work وSQL Server/EF Core.

## حالة المستودع

لا يحتوي فرع `main` على تنفيذ Feature قبل دمجه عبر Pull Request معتمد. يبدأ العمل الحالي في الفرع `feature/3.1-backend-foundation` وفق حزمة WBS `3.1`.

## قواعد غير قابلة للتجاوز

- لا يُدفع مباشرة إلى `main`؛ راجع [CONTRIBUTING.md](CONTRIBUTING.md).
- كل Use Case يمر عبر Command أو Query ومعالجة Application، ولا يستدعي Controller أو Hub قاعدة البيانات مباشرة.
- Jitsi معزول داخل Infrastructure فقط خلف `IMeetingMediaService` في Application.
- تفويض اعتماد ملخص AI يمر عبر سياسة Application قابلة للتوسعة، لا فحص دور ثابت داخل Handler.
- Swagger/OpenAPI هو مصدر عقد التكامل الحي مع Flutter.

## المرجع الحي

يشير هذا المستودع إلى [مستودع وثائق المشروع](https://github.com/sameer73-s/ultimatesolution-communication-docs) للدليل المعماري وADRs ومخططات المرحلة 1.
