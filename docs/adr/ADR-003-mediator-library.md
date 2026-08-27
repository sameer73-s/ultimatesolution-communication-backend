# ADR-003 — اعتماد Mediator بترخيص MIT لاستدعاءات CQRS

| الحقل | القيمة |
|---|---|
| الحالة | **مقبول** |
| التاريخ | 2026-08-28 |
| القرار | استبدال MediatR نهائيًا بـ `Mediator.Abstractions` و`Mediator.SourceGenerator` الإصدار `3.0.2` |

## السياق

يفرض الدليل المعماري للمشروع نمط CQRS عبر مكتبة وسيط ذات ترخيص متساهل ودائم من نوع MIT أو Apache-2.0، ويحظر حزمة NuGet الرسمية `MediatR` أو أي اعتماد مرخّص من Lucky Penny. كان تنفيذ WBS 3.2 يستعمل MediatR؛ لذلك يلزم استبداله قبل اعتماد Pull Request رقم 2.

## التحقق من المصدر والترخيص

تم التحقق مباشرة من المصدرين الرسميين للمشروع والحزم. يعرض مستودع المشروع الرسمي ترخيص **MIT** صراحة، كما ينص ملف `LICENSE` على منح حق الاستخدام والنسخ والتعديل والدمج والنشر والتوزيع والترخيص الفرعي والبيع، مع الالتزام فقط بإبقاء إشعار الحقوق والترخيص.[1] [2]

تؤكد بيانات NuGet الرسمية للحزمة `Mediator.Abstractions` بالإصدار `3.0.2` أن المشروع المنشئ هو `martinothamar/Mediator` وأن الاعتمادات المباشرة هي مكتبات Microsoft وBCL فقط، ولا تتضمن `MediatR` أو Lucky Penny.[3] أما `Mediator.SourceGenerator` بالإصدار نفسه فهو منشور من المشروع نفسه ويربط بالترخيص والمستودع نفسيهما.[4]

> هذا توثيق هندسي للتحقق من أصل الترخيص والاعتمادات المباشرة، وليس استشارة قانونية. ينبغي إعادة التحقق عند أي تحديث مستقبلي للحزم أو قبل اتخاذ قرار قانوني ذي أثر تعاقدي.

## القرار

يُعتمد ما يأتي في WBS 3.2 وما بعدها:

| الموقع | الحزمة أو الواجهة | سبب الموضع |
|---|---|---|
| `UltimateSolution.Application` | `Mediator.Abstractions` 3.0.2 | تعريف الأوامر والمعالجات وسلوك التحقق عبر `IRequest<TResponse>` و`IRequestHandler<TRequest, TResponse>` و`IPipelineBehavior<TMessage, TResponse>`. |
| `UltimateSolution.Application` | `Mediator.SourceGenerator` 3.0.2، كاعتماد تطويري خاص | يولّد `AddMediator()` وتنفيذ `IMediator` في التجميع الذي يحتوي تسجيل `AddApplication()`؛ لذلك لا يُثبّت مولد ثانٍ في API. |
| `UltimateSolution.API` | `Mediator.Abstractions` 3.0.2 | حقن `IMediator` في الـControllers وإرسال الأوامر إلى Application فقط. |

يبقى التسجيل محصورًا في `AddApplication()` كما يفرض الدليل. تستعمل واجهات الأوامر والمعالجات أسماء متوافقة تقريبًا مع السابقة (`IRequest` و`IRequestHandler`)، وتستخدم Controllers واجهة `IMediator` للإرسال. يختلف توقيع المعالج الداخلي في أن Mediator يعيد `ValueTask<TResponse>`، وهو تعديل محدود لا يغير سلوك Use Cases أو عقود HTTP.

## النتائج والضوابط

لا تُضاف MediatR أو أي حزمة من Lucky Penny مباشرة أو بشكل انتقالي. قبل اعتماد أي تحديث لـMediator، يجب فحص بيانات NuGet الرسمية لشجرة الاعتمادات وتشغيل `dotnet list <solution> package --include-transitive` للتحقق من غياب `MediatR` و`LuckyPenny`. كما يجب أن يمر البناء وجميع الاختبارات دون تحذير ترخيص.

## المراجع

[1]: https://github.com/martinothamar/Mediator "المستودع الرسمي لمكتبة Mediator"
[2]: https://raw.githubusercontent.com/martinothamar/Mediator/main/LICENSE "نص ترخيص MIT الرسمي لمكتبة Mediator"
[3]: https://api.nuget.org/v3-flatcontainer/mediator.abstractions/3.0.2/mediator.abstractions.nuspec "بيانات NuGet الرسمية لـ Mediator.Abstractions 3.0.2"
[4]: https://www.nuget.org/packages/Mediator.SourceGenerator/3.0.2 "صفحة NuGet الرسمية لـ Mediator.SourceGenerator 3.0.2"
