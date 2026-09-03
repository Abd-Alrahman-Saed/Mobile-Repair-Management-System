# خطة تطوير MRMS إلى نظام ERP كامل

> **الحالة:** مسودة تنفيذية · **التاريخ:** 2026-09-02 · **الفرع:** `main`
> **المرجع البصري:** [MRMS Redesign.dc.html](https://claude.ai/design/p/aadf48dc-5d51-4a0b-9098-13a24e638190?file=MRMS+Redesign.dc.html)
> **مرجع الأعطال الحالية:** [AUDIT.md](AUDIT.md)

---

## كيف تُقرأ الخطة دي

كل Phase مكتوبة بنفس الشكل:

| الجزء | المعنى |
|---|---|
| **الهدف** | جملة واحدة: إيه اللي هيتغير في المحل بعد المرحلة دي |
| **يعتمد على** | مراحل لازم تخلص قبلها |
| **المهام** | مهام مرقّمة، كل واحدة بملفاتها الفعلية |
| **معيار القبول** | اللي لازم يشتغل قدامك علشان تقول «خلصت» |

المهام مكتوبة بمسارات ملفات حقيقية من المستودع. أي ملف جديد مكتوب بعلامة `(جديد)`.

**ترتيب التنفيذ المقترح:** `0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9`
المراحل `0`، `2`، `4` هي العمود الفقري — الباقي يقدر يتأجل أو يتنفّذ بالتوازي.

---

## ملخص الديزاين الجديد

الديزاين عبارة عن **prototype تفاعلي كامل** بشاشة واحدة (single-window shell) فيها ١٦ شاشة، ومفتاح تبديل بين دورين (**مالك المحل / موظف الاستقبال**) بيغيّر القائمة والبيانات الظاهرة فعلياً.

### الـ Design Tokens المستخرجة

| Token | القيمة | المقابل الحالي في `ModernStyles.xaml` |
|---|---|---|
| `--color-accent` | `#2F6FED` | `PrimaryColor` = `#2563EB` → **يتغيّر** |
| `--color-accent-600` | `#2A5FD1` | `PrimaryHoverColor` = `#1D4ED8` → **يتغيّر** |
| `--color-accent-700` | `#1F4AA8` | جديد |
| `--color-accent-100` | `#EAF1FF` | `PrimaryLightColor` = `#DBEAFE` → **يتغيّر** |
| `--color-bg` | `#F2F2F3` | `BackgroundColor` = `#F1F5F9` → **يتغيّر** |
| `--color-surface-elevated` | `#FFFFFF` | `CardBackgroundColor` ✅ مطابق |
| `--color-text` | `#1D1F20` | `TextPrimaryColor` = `#0F172A` → **يتغيّر** |
| `--color-divider` | `#1D1F20` بشفافية `12%` | `BorderColor` = `#E2E8F0` → **يتغيّر** |
| Sidebar | تدرّج `#131D3A → #0A1226` | `SidebarColor` = `#1E293B` → **يتغيّر لتدرّج** |
| `--radius-sm/md/lg` | `8px / 10px / 16px` | متفرّقة في الأنماط → **توحيد** |
| `--shadow-sm/md/lg` | `0 1px 2px` · `0 3px 10px` · `0 12px 32px` بلون `#2B2B2D` | `CardShadow` / `ElevatedShadow` → **إعادة ضبط** |
| الخط | **Cairo** أوزان `400,500,600,700,800` | الافتراضي حالياً → **يتغيّر** |

### ألوان حالات التذكرة (من الديزاين حرفياً)

| الحالة | التسمية | خلفية فاتحة | نص | لون العمود |
|---|---|---|---|---|
| `Received` | مستلم | `#F5F5F8` | `#424244` | `#98989B` |
| `UnderInspection` | **قيد الفحص** | `#DBEAFE` | `#1E4E79` | `#3B8BC2` |
| `WaitingForParts` | بانتظار القطع | `#FEF3C7` | `#B45309` | `#D97706` |
| `InRepair` | قيد الإصلاح | `#BFDCF0` | `#124467` | `#1D6FA5` |
| `Repaired` | تم الإصلاح | `#DCFCE7` | `#16A34A` | `#22C55E` |
| `Delivered` | تم التسليم | `#BBF7D0` | `#166534` | `#16A34A` |
| `Cancelled` | ملغي | `#FEE2E2` | `#DC2626` | `#DC2626` |

> **ملاحظة:** الديزاين اعتمد **«قيد الفحص»** لـ `UnderInspection`. حالياً الكود فيه تسميتين متضاربتين (`قيد الفحص` في المحوّل والقائمة، `تحت الفحص` في شاشة التفاصيل والطباعة) — الديزاين حسم الخلاف، والتوحيد في **Phase 1 / مهمة 1.4**.

### الشاشات في الديزاين

| # | الشاشة | مفتاح | متاحة لـ | الحالة في الكود |
|---|---|---|---|---|
| 1 | لوحة التحكم | `dashboard` | الكل | موجودة — تتحدّث |
| 2 | تذاكر الإصلاح (**Kanban + جدول**) | `tickets` | الكل | جدول فقط — **Kanban جديد** |
| 3 | تفاصيل التذكرة | `ticketDetail` | الكل | موجودة — تتحدّث |
| 4 | استقبال جهاز (**wizard ٣ خطوات**) | `intake` | الكل | مقسّمة على شاشتين — **تتوحّد** |
| 5 | نقل ملكية | `transfer` | الكل | موجودة |
| 6 | العملاء (**+ عمود مديونية**) | `customers` | الكل | موجودة — **عمود جديد** |
| 7 | المبيعات | `sales` | الكل | موجودة — الربح للمالك فقط |
| 8 | المشتريات | `purchases` | الكل | موجودة |
| 9 | المخزون | `inventory` | الكل | موجودة — التكلفة للمالك فقط |
| 10 | الموردون | `suppliers` | الكل | موجودة — الرصيد للمالك فقط |
| 11 | **درج النقدية** | `cashRegister` | **المالك فقط** | ❌ **غير موجودة** |
| 12 | دفعات الموردين | `supplierPayments` | **المالك فقط** | موجودة |
| 13 | المصروفات | `expenses` | **المالك فقط** | موجودة |
| 14 | التقارير | `reports` | **المالك فقط** | موجودة (٣ شاشات) |
| 15 | بحث | `search` | الكل | موجودة |
| 16 | الإعدادات | `settings` | **المالك فقط** | موجودة |

### إضافات وظيفية جاية من الديزاين مباشرة

1. **مفتاح الدور في الـ Sidebar** — `adminOnlyScreens = [cashRegister, supplierPayments, expenses, reports, settings]`
2. **شريط علوي** فيه بحث شامل (رقم تذكرة / اسم عميل / IMEI) + **جرس تنبيهات** بنقطة حمراء
3. **قائمة تنبيهات** — نفذ من المخزون، مخزون منخفض، تذكرة راكدة بانتظار القطع
4. **لوحة Kanban** للتذاكر بـ ٧ أعمدة ملوّنة
5. **تخصيص فني** على التذكرة (`technicianAssignments`)
6. **زر «إبلاغ العميل»** على تذكرة جاهزة، مع حالة «تم الإبلاغ»
7. **خط زمني للحالات** (`timeline`) في شاشة التفاصيل
8. **درج النقدية**: رصيد افتتاحي، مبيعات، مصروف، متوقّع، معدود، **الفرق**، إقفال يومية + سجل الإقفالات
9. **مديونية العميل** (`debt`) في جدول العملاء
10. **نوع صنف ثالث: «خدمة»** برصيد `null` (زي «خدمة تركيب شاشة»)
11. **بطاقتا KPI للمالك فقط** في لوحة التحكم: صافي الربح + المستحق للموردين

> **فارق مقصود بين الديزاين وخطتي السابقة:** الديزاين حافظ على **الحالات السبع الحالية** ومضافش مراحل «موافقة العميل» و«الفحص النهائي». الخطة دي بتنفّذ الديزاين كما هو في Phase 4، وبتحط المراحل الإضافية في **Phase 4-B (اختياري)** — لأنها بتغيّر شكل الـ Kanban (٩ أعمدة بدل ٧) ومحتاجة قرار منك.

---

## جدول المراحل

| Phase | العنوان | الأثر | الاعتماد |
|---|---|---|---|
| **0** | إصلاح الأساس المكسور | يمنع فقدان بيانات | — |
| **1** | الهوية البصرية الجديدة | الشكل | 0 |
| **2** | المستخدمون والأدوار والصلاحيات | **الأهم للبيع** | 0 |
| **3** | الشيل الجديد: Sidebar + Topbar + تنبيهات | الشكل + التنقّل | 1، 2 |
| **4** | ورك فلو الصيانة | **جوهر المنتج** | 2، 3 |
| **4-B** | مراحل الصيانة الموسّعة *(اختياري)* | جودة الخدمة | 4 |
| **5** | درج النقدية والوردية | **حماية الفلوس** | 2، 3 |
| **6** | فاتورة مبيعات ومديونية العملاء | إيراد | 2، 5 |
| **7** | المخزون والتنبيهات | تشغيل | 3 |
| **8** | التقارير والتصدير | إدارة | 2 |
| **9** | التجهيز للبيع كمنتج | التسويق | الكل |

---

# Phase 0 — إصلاح الأساس المكسور

**الهدف:** إن أي حاجة تتكتب في البرنامج تفضل موجودة، وأي تعديل على الجداول يوصل لأي نسخة مركّبة.

**يعتمد على:** لا شيء. **دي أول حاجة تتعمل — أي شغل قبلها هيتبني على أرض مكسورة.**

### المهام

#### 0.1 — إصلاح ضياع القيود المحاسبية

المشكلة: `AppDbContext` و`IUnitOfWork` مسجّلين `Transient` في [MRMS.Infrastructure/DependencyInjection.cs](MRMS.Infrastructure/DependencyInjection.cs). يعني `RepairTicketService` بياخد نسخة، و`JournalService` اللي جواه بياخد نسخة تانية. القيد بيتضاف لسياق، والحفظ بيحصل على سياق تاني — فالقيد بيضيع من غير أي رسالة خطأ.

- [ ] تغيير التسجيل من `Transient` لـ `Scoped` في `AddInfrastructure`
- [ ] إضافة `IServiceScopeFactory` في `MRMS.UI/Services/NavigationService.cs` بحيث كل انتقال لصفحة يفتح scope جديد
- [ ] أو (البديل الأبسط لو الـ scoping معقّد): تسجيل `AppDbContext` كـ `Scoped` وتمرير نفس الـ `IUnitOfWork` لكل الخدمات داخل العملية الواحدة
- [ ] التحقّق: بيع صنف واحد → فتح جدول `JournalEntries` → لازم يكون فيه صف جديد بسطرين

**الملفات:** `MRMS.Infrastructure/DependencyInjection.cs` · `MRMS.UI/App.xaml.cs` · `MRMS.UI/Services/NavigationService.cs`

#### 0.2 — معاملات قاعدة بيانات حقيقية

مفيش `BeginTransaction` ولا `TransactionScope` في المشروع كله. أي عملية بتكتب في أكتر من جدول (بيع = صف مبيعات + خصم رصيد + قيد) لو وقعت في النص، بتسيب نص العملية محفوظ.

- [ ] إضافة `Task ExecuteInTransactionAsync(Func<Task> work)` في `IUnitOfWork` / `UnitOfWork`
- [ ] لفّ العمليات دي بالمعاملة:
  - `SaleService.CreateAsync`
  - `PurchaseService.CreateAsync`
  - `RepairTicketService.UpdateStatusAsync` (عند `Delivered`)
  - `StockMovementService.CreateAsync`
  - `SupplierPaymentService.CreateAsync`

**الملفات:** `MRMS.Core/Interfaces/IUnitOfWork.cs` · `MRMS.Infrastructure/Repositories/UnitOfWork.cs` · `MRMS.Application/Services/*.cs`

#### 0.3 — رجوع نظام الترحيلات (Migrations)

مجلد `MRMS.Infrastructure/Migrations/` **محذوف بالكامل**، والتشغيل معتمد على `EnsureCreatedAsync()` في [MRMS.Infrastructure/Services/SeedService.cs:27](MRMS.Infrastructure/Services/SeedService.cs). `EnsureCreated` بيعمل الجداول أول مرة بس — أي عمود جديد بعد كده **مش هيتطبّق على أي نسخة شغالة عند عميل، ومن غير أي رسالة خطأ**.

كل المراحل الجاية بتضيف أعمدة وجداول. من غير الخطوة دي، مفيش حاجة منهم هتوصل لعميل عنده نسخة قديمة.

- [ ] `dotnet ef migrations add InitialCreate --project MRMS.Infrastructure --startup-project MRMS.UI`
- [ ] استبدال `EnsureCreatedAsync()` بـ `Database.MigrateAsync()` في `SeedService`
- [ ] لقواعد البيانات الموجودة فعلاً: سكربت يملأ جدول `__EFMigrationsHistory` بالـ `InitialCreate` علشان ميحاولش يعمل الجداول تاني
- [ ] توثيق الخطوة دي في `README.md`

**الملفات:** `MRMS.Infrastructure/Migrations/` (جديد) · `MRMS.Infrastructure/Services/SeedService.cs` · `README.md`

#### 0.4 — إصلاح مشروع الاختبارات

`dotnet build` بيفشل بـ **٤ أخطاء** في [MRMS.Tests/Services/RepairTicketServiceTests.cs](MRMS.Tests/Services/RepairTicketServiceTests.cs) — الاختبارات لسه بتنادي `new RepairTicketService(uow)` بينما الخدمة بقت بتاخد `(IUnitOfWork, IJournalService)`.

- [ ] تمرير `Mock<IJournalService>` في الأربع مواضع (سطور 24، 50، 77، 101)
- [ ] `dotnet test` يعدّي أخضر
- [ ] اختبارات جديدة لكل حاجة اتصلحت في `0.1` و`0.2`

**الملفات:** `MRMS.Tests/Services/RepairTicketServiceTests.cs`

#### 0.5 — نقل صور الأجهزة لمجلد مُدار

الصورة دلوقتي بتتسجّل بمسارها الأصلي من `OpenFileDialog` (مثلاً من سطح المكتب). أول ما الملف يتنقل أو يتمسح، الصورة تروح — وهي الدليل الوحيد على حالة الجهاز عند الاستلام.

- [ ] نسخ الملف لـ `%AppData%\MRMS\Photos\{TicketId}\{Guid}.jpg` وقت الإضافة
- [ ] تخزين مسار نسبي في `DevicePhoto.FilePath` بدل المسار المطلق
- [ ] سكربت ترحيل للصور الموجودة (لو الملف الأصلي لسه موجود)

**الملفات:** `MRMS.Application/Services/PhotoService.cs` · `MRMS.UI/Views/RepairTicketDetailPage.xaml.cs`

#### 0.6 — تسجيل الأخطاء

٢٤+ موضع بيمسك `Exception` ويعرض `ex.Message` خام في واجهة عربية، وتقريباً محدش فيهم بيسجّل. أي عطل في الإنتاج بيختفي من غير أثر في `logs/mrms-*.log`.

- [ ] `Log.Error(ex, ...)` في كل `catch` في `MRMS.UI/ViewModels/`
- [ ] رسالة عربية مفهومة للمستخدم بدل نص الاستثناء الخام

**الملفات:** `MRMS.UI/ViewModels/*.cs`

### معيار القبول — Phase 0

- ✅ `dotnet build` و`dotnet test` أخضر بالكامل
- ✅ بيع صنف → صف في `JournalEntries` بسطرين متوازنين
- ✅ `dotnet ef migrations list` بيرجّع ترحيل واحد على الأقل، والتشغيل بيعدّي عليه
- ✅ إضافة صورة لتذكرة ثم نقل الملف الأصلي → الصورة لسه بتظهر

---

# Phase 1 — الهوية البصرية الجديدة

**الهدف:** البرنامج يبقى شكله زي الديزاين — من غير ما يتغيّر أي منطق شغل.

**يعتمد على:** Phase 0

### المهام

#### 1.1 — إعادة ضبط الـ Tokens

`MRMS.UI/Styles/ModernStyles.xaml` فيه بالفعل نظام tokens (٢٢ لون + brushes + ٣ ظلال). المطلوب **إعادة ضبط قيمهم** على الديزاين، مش إعادة كتابة النظام.

- [ ] تحديث الألوان حسب الجدول في «الـ Design Tokens المستخرجة» فوق
- [ ] إضافة `AccentColor700` = `#1F4AA8` و`SurfaceMutedColor` = `#E9E9EA`
- [ ] `SidebarBrush` يتحوّل من `SolidColorBrush` إلى `LinearGradientBrush` (`#131D3A` → `#0A1226`، رأسي)
- [ ] توحيد أنصاف الأقطار: `RadiusSm=8` · `RadiusMd=10` · `RadiusLg=16`
- [ ] إعادة ضبط `CardShadow` / `ElevatedShadow` على قيم `--shadow-sm` / `--shadow-md`

**الملفات:** `MRMS.UI/Styles/ModernStyles.xaml`

#### 1.2 — خط Cairo

- [ ] إضافة ملفات `Cairo-*.ttf` (أوزان 400/500/600/700/800) تحت `MRMS.UI/Resources/Fonts/` كـ `Resource`
- [ ] `FontFamily` عام في `App.xaml` يشير للخط المدمج
- [ ] سلّم أحجام موحّد: `28px` عنوان صفحة · `26px` KPI · `17px` عنوان قسم · `14px` نص · `12.5px` جدول · `11px` تسمية

**الملفات:** `MRMS.UI/Resources/Fonts/` (جديد) · `MRMS.UI/App.xaml` · `MRMS.UI/Styles/ModernStyles.xaml`

#### 1.3 — أنماط المكوّنات

الديزاين فيه مكتبة مكوّنات صغيرة (`.btn`, `.card`, `.tag`, `.table`, `.input`, `.seg`). المقابل في WPF:

- [ ] `PrimaryButton` / `SecondaryButton` / `GhostButton` (جديد) / `BlockButton` (جديد)
- [ ] `CardBorder` — خلفية بيضاء + حد `divider` + ظل `sm` + نصف قطر `10`
- [ ] `StatusTag` (جديد) — `Border` بنصف قطر `999` وحشو `4px 12px` ووزن `600`
- [ ] `ModernDataGrid` — رأس بخلفية `#F5F5F8`، حروف كبيرة، تباعد `0.08em`
- [ ] `SegmentedControl` (جديد) — لتبديل «لوحة / قائمة» و«هذا الشهر / الشهر الماضي»

**الملفات:** `MRMS.UI/Styles/ModernStyles.xaml` · `MRMS.UI/Controls/` (جديد)

#### 1.4 — مصدر واحد لتسميات وألوان الحالة

التسمية العربية لحالات التذكرة مكرّرة في **٤ أماكن**، واتنين منهم مختلفين عن التانيين (`قيد الفحص` مقابل `تحت الفحص`).

- [ ] إنشاء `MRMS.UI/Resources/TicketStatusPalette.cs` (جديد): تسمية + لون خلفية + لون نص + لون عمود لكل حالة، من جدول الديزاين
- [ ] `TicketStatusConverter` يقرأ منه
- [ ] حذف الـ `switch` المكرّرة من:
  - `MRMS.UI/ViewModels/RepairTicketListViewModel.cs`
  - `MRMS.UI/ViewModels/RepairTicketViewModel.cs`
  - `MRMS.UI/Views/PrintPreviewWindow.xaml.cs`
- [ ] **التسمية المعتمدة:** «قيد الفحص»

**الملفات:** `MRMS.UI/Resources/TicketStatusPalette.cs` (جديد) · `MRMS.UI/Converters/TicketStatusConverter.cs` · ٣ ملفات الحذف فوق

#### 1.5 — تطبيق على كل الشاشات

- [ ] مرور على ٣٠ ملف `.xaml` في `MRMS.UI/Views/` وإحلال القيم المباشرة بالـ tokens
- [ ] توحيد حشو الصفحة على `28px` (زي `padding:28px` في الديزاين)
- [ ] توحيد المسافة بين البطاقات على `16px`

**الملفات:** `MRMS.UI/Views/*.xaml`

### معيار القبول — Phase 1

- ✅ فتح البرنامج جنب لقطة الديزاين — نفس الألوان والخط والمسافات
- ✅ `grep -r "#" MRMS.UI/Views/*.xaml` مفيش ألوان مكتوبة مباشرة خارج الأنماط
- ✅ «قيد الفحص» ظاهرة بنفس النص في القائمة والتفاصيل والطباعة

---

# Phase 2 — المستخدمون والأدوار والصلاحيات

**الهدف:** كل واحد يدخل باسمه، ويشوف اللي يخصّه بس، وكل عملية حسّاسة يفضل عليها اسم صاحبها.

**يعتمد على:** Phase 0

> **دي أهم مرحلة تجارياً.** من غيرها النظام مش قابل للبيع لأي محل فيه أكتر من شخص. الديزاين نفسه مبني حواليها — مفتاح الدور في الـ Sidebar هو أول عنصر تحت اللوجو.

### المهام

#### 2.1 — نموذج البيانات

- [ ] `MRMS.Core/Enums/UserRole.cs` (جديد): `Owner`, `Manager`, `Receptionist`, `Technician`
- [ ] `MRMS.Core/Enums/Permission.cs` (جديد): تعداد بالصلاحيات المفصّلة (`ViewCostPrice`, `ViewProfit`, `ViewCashRegister`, `ManageSuppliers`, `ApplyDiscount`, `VoidTransaction`, `ManageUsers`, `Backup`, …)
- [ ] تعديل `MRMS.Core/Entities/User.cs`: `FullName`, `Role`, `IsActive`, `PasswordSalt`, `LastLoginAt`
- [ ] `MRMS.Core/Entities/AuditLog.cs` (جديد): `Id`, `UserId`, `Action`, `EntityType`, `EntityId`, `Details`, `CreatedAt`
- [ ] تسجيل الجداول في `AppDbContext` + ترحيل

**الملفات:** `MRMS.Core/Enums/` · `MRMS.Core/Entities/` · `MRMS.Infrastructure/Data/AppDbContext.cs`

#### 2.2 — تسجيل دخول حقيقي

`AuthService` حالياً بيقارن **الباسورد لوحده** من غير اسم مستخدم، والتهشيم `SHA-256` بدون Salt. فيه كومنت في الكود بيقول إن ده مقصود («باسورد واحد للمحل») — ده اللي بيتغيّر هنا.

- [ ] `LoginAsync(username, password)` — مطابقة على الاسم أولاً
- [ ] تهشيم `PBKDF2` بـ salt عشوائي لكل مستخدم (`Rfc2898DeriveBytes`، ‏100k تكرار، `SHA256`)
- [ ] ترحيل الباسورد القديم: أول دخول ناجح بالطريقة القديمة → إعادة تهشيمه بالجديدة
- [ ] `SeedService` يعمل حساب `owner` واحد ويطلب تغيير الباسورد أول تشغيل — **مفيش باسورد افتراضي في الكود**
- [ ] `MRMS.Application/Services/CurrentUserService.cs` (جديد) — singleton فيه المستخدم الحالي ودوره
- [ ] تحديث `LoginWindow.xaml` بخانة اسم مستخدم

**الملفات:** `MRMS.Application/Services/AuthService.cs` · `MRMS.Infrastructure/Services/SeedService.cs` · `MRMS.UI/Views/LoginWindow.xaml` · `MRMS.UI/ViewModels/LoginViewModel.cs`

#### 2.3 — التطبيق في طبقة الخدمة (مش في الواجهة)

> **القاعدة:** إخفاء زرار مش حماية. لو حد وصل للشاشة بأي طريقة، **الخدمة نفسها** لازم ترفض.

- [ ] `MRMS.Application/Services/PermissionService.cs` (جديد): `bool Can(Permission p)` + `void Require(Permission p)` بترمي `UnauthorizedAccessException`
- [ ] `Require(...)` في بداية كل خدمة حسّاسة:
  - `SupplierPaymentService` · `ExpenseService` · `IncomeStatementService` · `MonthlyReportService` · `ReportService` · `JournalService` · `SystemSettingService` · `IBackupService`
- [ ] كل عملية حسّاسة تكتب صف في `AuditLog`

**الملفات:** `MRMS.Application/Services/PermissionService.cs` (جديد) · كل ملفات `MRMS.Application/Services/`

#### 2.4 — حجب الحقول على مستوى الـ DTO

الاستقبال محتاج شاشة الأصناف علشان يبيع — لكن **مايشوفش التكلفة ولا الربح**. الحجب لازم يحصل في الـ DTO قبل ما يوصل للواجهة، مش بـ Converter في الـ XAML.

- [ ] `ItemService.GetAllAsync` — `CostPrice = null` لو `!Can(ViewCostPrice)`
- [ ] `SaleService` — `Profit = null` و`CostPrice = null` لو `!Can(ViewProfit)`
- [ ] `SupplierService` — `Balance = null` لو `!Can(ViewSupplierBalance)`
- [ ] `DashboardService` — بطاقتا «صافي الربح» و«المستحق للموردين» ترجع `null` لغير المالك *(مطابق لـ `sc-if isAdmin` في الديزاين)*
- [ ] تحويل الحقول دي لـ `decimal?` في الـ DTOs المتأثرة

**الملفات:** `MRMS.Application/DTOs/ItemDto.cs` · `SaleDto.cs` · `SupplierDto.cs` · `DashboardStatsDto.cs` + خدماتهم

#### 2.5 — حدود الخصم وموافقة المالك

- [ ] إعدادات جديدة في `SystemSetting`: `MaxDiscountPercent.Receptionist` · `MaxDiscountPercent.Manager`
- [ ] رفض أي خصم فوق الحد
- [ ] **رفض أي سعر بيع أقل من التكلفة** لغير المالك — من غير ما يعرض التكلفة للموظف
- [ ] `MRMS.UI/Views/OwnerApprovalDialog.xaml` (جديد): نافذة تطلب كود المالك لحظياً، والعملية تتسجّل باسم الاتنين

**الملفات:** `MRMS.Application/Services/SaleService.cs` · `SystemSettingService.cs` · `MRMS.UI/Views/OwnerApprovalDialog.xaml` (جديد)

#### 2.6 — إلغاء بدل حذف

- [ ] `IsVoided` + `VoidedByUserId` + `VoidReason` + `VoidedAt` على `Sale`, `Purchase`, `Expense`, `SupplierPayment`
- [ ] الإلغاء بيسجّل **قيد عكسي** بدل ما يمسح الصف
- [ ] الحركات الملغاة تفضل ظاهرة للمالك ومستبعدة من التقارير

**الملفات:** `MRMS.Core/Entities/*.cs` · `MRMS.Application/Services/*.cs`

#### 2.7 — شاشة إدارة المستخدمين

- [ ] `MRMS.UI/Views/UsersPage.xaml` + `UserFormPage.xaml` (جديد)
- [ ] `MRMS.UI/ViewModels/UsersViewModel.cs` + `UserFormViewModel.cs` (جديد)
- [ ] `MRMS.UI/Views/AuditLogPage.xaml` (جديد) — للمالك فقط، مع فلترة بالمستخدم والتاريخ ونوع العملية

**الملفات:** `MRMS.UI/Views/` · `MRMS.UI/ViewModels/`

### مصفوفة الصلاحيات

| الصلاحية | المالك | مدير | استقبال | فني |
|---|:--:|:--:|:--:|:--:|
| استلام جهاز وفتح تذكرة | ✅ | ✅ | ✅ | ❌ |
| تحديث حالة التذكرة | ✅ | ✅ | استلام وتسليم فقط | ✅ |
| تسعير الصيانة | ✅ | ✅ | ❌ | اقتراح فقط |
| تحصيل وتسليم جهاز | ✅ | ✅ | ✅ | ❌ |
| بيع إكسسوارات بسعر القائمة | ✅ | ✅ | ✅ | ❌ |
| خصم فوق الحد | ✅ | حتى حدّه | ❌ | ❌ |
| **رؤية سعر التكلفة والربح** | ✅ | ✅ | ❌ | ❌ |
| قائمة الدخل والتقرير الشهري | ✅ | فرعه | ❌ | ❌ |
| **درج النقدية** | ✅ | ✅ | ورديته فقط | ❌ |
| الموردون وأرصدتهم والدفعات | ✅ | ✅ | ❌ | ❌ |
| المشتريات وإدخال بضاعة | ✅ | ✅ | ❌ | ❌ |
| تعديل أسعار الأصناف | ✅ | بيع فقط | ❌ | ❌ |
| صرف قطع غيار على تذكرة | ✅ | ✅ | ❌ | ✅ |
| جرد وتسوية مخزون | ✅ | ✅ | ❌ | ❌ |
| إلغاء حركة بعد ترحيلها | ✅ | بموافقة المالك | ❌ | ❌ |
| حذف عميل / جهاز / صنف | ✅ | ❌ | ❌ | ❌ |
| إدارة المستخدمين | ✅ | ❌ | ❌ | ❌ |
| نسخ احتياطي واستعادة | ✅ | ❌ | ❌ | ❌ |
| سجل التدقيق | ✅ | ❌ | ❌ | ❌ |

### معيار القبول — Phase 2

- ✅ دخول بحساب استقبال → القائمة مفيهاش: درج النقدية، دفعات موردين، مصروفات، تقارير، إعدادات
- ✅ شاشة الأصناف بحساب استقبال → عمود «سعر التكلفة» **مش موجود** (مش مخفي بـ Visibility)
- ✅ محاولة استدعاء `IncomeStatementService` بحساب استقبال → `UnauthorizedAccessException`
- ✅ لوحة التحكم بحساب استقبال → ٥ بطاقات بدل ٧
- ✅ أي بيع / إلغاء / تعديل سعر → صف في `AuditLog` باسم المستخدم

---

# Phase 3 — الشيل الجديد: Sidebar + Topbar + تنبيهات

**الهدف:** التنقّل والشكل العام يبقوا زي الديزاين، والقائمة تتفلتر تلقائياً حسب الدور.

**يعتمد على:** Phase 1، Phase 2

### المهام

#### 3.1 — الـ Sidebar

- [ ] عرض `264px`، خلفية متدرّجة `#131D3A → #0A1226`، ظل `-10px 0 28px -14px`
- [ ] رأس: مربّع `36px` بنصف قطر `10` وتدرّج accent فيه `KMG` + «مركز الصيانة» + سطر وصف
- [ ] **بطاقة المستخدم** أسفل القائمة: الحرف الأول في دائرة + الاسم + **الدور** + زر خروج
- [ ] الأقسام حسب الديزاين: لوحة التحكم · تذاكر · استقبال جهاز · نقل ملكية · العملاء · المبيعات · المشتريات · المخزون · الموردون · [دفعات · مصروفات · تقارير · درج النقدية] · بحث · [إعدادات]
- [ ] **فلترة عناصر القائمة بـ `PermissionService`** — العنصر مش بيتبني أصلاً لو الدور مش مصرّح له
- [ ] لو المستخدم كان في شاشة ممنوعة ودوره اتغيّر → تحويل تلقائي للوحة التحكم *(زي `setRoleStaff` في الديزاين)*

**الملفات:** `MRMS.UI/Views/HomePage.xaml` + `.cs` · `MRMS.UI/ViewModels/` (جديد: `ShellViewModel.cs`)

#### 3.2 — الشريط العلوي

- [ ] خلفية بيضاء، حشو `16px 32px`، ظل `0 1px 0 rgba(15,23,42,.06)`
- [ ] **بحث شامل** بعرض `460px` وأيقونة عدسة على اليمين: يبحث في رقم التذكرة + اسم العميل + IMEI في نفس الوقت
- [ ] نتائج البحث بنفس شكل `searchResults` في الديزاين (عنوان + سطر فرعي + نوع)

**الملفات:** `MRMS.UI/Views/MainWindow.xaml` · `MRMS.UI/Views/HomePage.xaml`

#### 3.3 — التنبيهات

- [ ] `MRMS.Application/Services/AlertService.cs` (جديد) بيرجّع:
  - أصناف نفدت (`CurrentStock == 0`)
  - أصناف تحت حد الطلب (`CurrentStock <= ReorderLevel`)
  - تذاكر راكدة في `WaitingForParts` أكتر من N يوم (N في الإعدادات)
- [ ] جرس في الشريط العلوي + **نقطة حمراء `8px`** لما يكون فيه تنبيهات
- [ ] قائمة منسدلة `320px` بنصف قطر `14` — كل تنبيه: نقطة ملوّنة + عنوان + سطر فرعي
- [ ] حالة فاضية: «لا توجد تنبيهات جديدة»

**الملفات:** `MRMS.Application/Services/AlertService.cs` (جديد) · `MRMS.UI/Controls/NotificationPopup.xaml` (جديد)

#### 3.4 — لوحة التحكم الجديدة

- [ ] ٥ بطاقات KPI للكل: مستلم اليوم · تم التسليم اليوم · قيد الإصلاح · بانتظار القطع · مخزون منخفض
- [ ] **بطاقتان إضافيتان للمالك فقط**: صافي الربح · المستحق للموردين *(بلون `#7C3AED`)*
- [ ] كل بطاقة: kicker صغير + أيقونة `38px` في مربّع ملوّن + رقم `30px` وزن `800`
- [ ] جدول «آخر التذاكر» (٦ صفوف) + زر «عرض كل التذاكر»

**الملفات:** `MRMS.UI/Views/DashboardPage.xaml` · `MRMS.UI/ViewModels/DashboardViewModel.cs` · `MRMS.Application/Services/DashboardService.cs`

### معيار القبول — Phase 3

- ✅ الشيل مطابق للديزاين بصرياً في الدورين
- ✅ التبديل بين حساب مالك وحساب استقبال بيغيّر القائمة فعلياً
- ✅ تقليل رصيد صنف تحت حد الطلب → نقطة حمراء على الجرس فوراً
- ✅ البحث بـ IMEI جزئي بيرجّع الجهاز وتذاكره

---

# Phase 4 — ورك فلو الصيانة

**الهدف:** التذاكر تتدار من لوحة Kanban، والاستقبال يبقى خطوة واحدة، وكل تذكرة ليها فني ومسار محكوم.

**يعتمد على:** Phase 2، Phase 3

### المهام

#### 4.1 — لوحة Kanban

- [ ] `MRMS.UI/Views/RepairTicketListPage.xaml` — تبديل **لوحة / قائمة** بـ `SegmentedControl`
- [ ] `ItemsControl` أفقي بـ ٧ أعمدة، كل عمود `min-width:230px` / `max-width:250px`
- [ ] رأس العمود بلون `solid` من `TicketStatusPalette` + العدّاد
- [ ] بطاقة التذكرة: رقم (وزن 600) · `العميل · الجهاز` · المشكلة · التاريخ
- [ ] تمرير عمودي داخل العمود بحد أقصى `560px`
- [ ] الضغط على بطاقة → شاشة التفاصيل

**الملفات:** `MRMS.UI/Views/RepairTicketListPage.xaml` · `MRMS.UI/ViewModels/RepairTicketListViewModel.cs`

#### 4.2 — جدول انتقالات محكوم

المشكلة الحالية: القاعدة الوحيدة المطبّقة في [RepairTicketService.UpdateStatusAsync](MRMS.Application/Services/RepairTicketService.cs) هي «متغيّرش حاجة بعد `Delivered` أو `Cancelled`». أي انتقال تاني مسموح — تقدر تروح من `Received` لـ `Delivered` على طول. وده اللي بيسبّب انحراف المخزون.

- [ ] `MRMS.Core/TicketStateMachine.cs` (جديد): `IReadOnlyList<TicketStatus> AllowedNext(TicketStatus current)`
- [ ] المسار: `Received → {UnderInspection, Cancelled}` · `UnderInspection → {WaitingForParts, InRepair, Cancelled}` · `WaitingForParts → {InRepair, Cancelled}` · `InRepair → {Repaired, WaitingForParts, Cancelled}` · `Repaired → {Delivered, InRepair}` · `Delivered` و`Cancelled` نهائيتان
- [ ] `UpdateStatusAsync` بترفض أي انتقال خارج الجدول
- [ ] القائمة المنسدلة في شاشة التفاصيل بتعرض **الحالات المسموحة بس**
- [ ] اختبارات وحدة لكل انتقال مسموح وكل انتقال ممنوع

**الملفات:** `MRMS.Core/TicketStateMachine.cs` (جديد) · `MRMS.Application/Services/RepairTicketService.cs` · `MRMS.Tests/`

#### 4.3 — إصلاح خصم قطع الغيار

مشكلتان في نفس السطر:
1. الخصم بيحصل عند الدخول لحالة `InRepair` بس. تذكرة عدّت من غير ما تمر بالحالة دي = **القطعة اتركّبت وفضلت في الرصيد**.
2. الرجوع لـ `InRepair` تاني بيخصم **كل** القطع من الأول مرة تانية = **خصم مزدوج**.

- [ ] إضافة `IsStockDeducted` + `DeductedAt` على `RepairTicketItem`
- [ ] **نقل الخصم من حالة التذكرة إلى بند القطعة نفسه** — الخصم يحصل لحظة إضافة القطعة، والإرجاع لحظة حذفها
- [ ] `DeductPartsStockAsync` تتحوّل لعملية على البنود اللي `IsStockDeducted == false` فقط
- [ ] سكربت تسوية للبيانات الموجودة: أي بند على تذكرة `Repaired` أو `Delivered` ومتخصمش → خصمه ككل مع حركة تسوية موثّقة
- [ ] منع الرصيد السالب

**الملفات:** `MRMS.Core/Entities/RepairTicketItem.cs` · `MRMS.Application/Services/RepairTicketService.cs`

#### 4.4 — إزالة التغيير الضمني للحالة

`UpdateRepairInfoAsync` دلوقتي **بتفرض الحالة `Repaired`** كأثر جانبي لحفظ الوصف والتكلفة. يعني فني بيكتب ملاحظات في نص الشغل بيقلب التذكرة لـ «تم الإصلاح» من غير ما يقصد.

- [ ] فصل «حفظ معلومات الإصلاح» عن «تغيير الحالة» تماماً
- [ ] الحالة تتغيّر من زر «تحديث الحالة» بس

**الملفات:** `MRMS.Application/Services/RepairTicketService.cs` · `MRMS.UI/ViewModels/RepairTicketViewModel.cs`

#### 4.5 — تخصيص فني

- [ ] `AssignedTechnicianId` على `RepairTicket` (FK لـ `User` بدور `Technician`)
- [ ] قائمة منسدلة في شاشة التفاصيل *(زي `onTechnicianChange` في الديزاين)*
- [ ] فلتر بالفني في شاشة التذاكر
- [ ] تقرير إنتاجية أساسي: عدد التذاكر لكل فني + متوسط زمن الإصلاح

**الملفات:** `MRMS.Core/Entities/RepairTicket.cs` · `MRMS.UI/Views/RepairTicketDetailPage.xaml`

#### 4.6 — استقبال جهاز في شاشة واحدة

الديزاين وحّد `DeviceRegistrationPage` و`RepairTicketCreatePage` في **wizard بثلاث حالات**: `search` → `found` / `new`.

- [ ] `MRMS.UI/Views/IntakePage.xaml` (جديد) بثلاث حالات
- [ ] بحث بالـ IMEI → لو موجود يعرض العميل والجهاز، لو مش موجود يفتح تسجيل عميل + جهاز في نفس الشاشة
- [ ] زر واحد في الآخر: «إنشاء تذكرة الصيانة» → يوجّه للوحة Kanban
- [ ] إبقاء الشاشتين القديمتين مؤقتاً لحد ما الجديدة تستقر

**الملفات:** `MRMS.UI/Views/IntakePage.xaml` (جديد) · `MRMS.UI/ViewModels/IntakeViewModel.cs` (جديد)

#### 4.7 — الخط الزمني للحالات

- [ ] شريط أفقي في شاشة التفاصيل بيعرض الحالات السبع، الحالات المنقضية بلون `solid` والقادمة بـ `#D4D4D7` *(مطابق لـ `timeline` في الديزاين)*

**الملفات:** `MRMS.UI/Views/RepairTicketDetailPage.xaml`

#### 4.8 — إبلاغ العميل

- [ ] زر «إبلاغ العميل» على التذكرة الجاهزة
- [ ] يفتح `wa.me/<phone>` برسالة عربية جاهزة فيها رقم التذكرة والجهاز والمبلغ
- [ ] `NotifiedAt` على التذكرة + عرض حالة «تم الإبلاغ» بعدها *(زي `notifiedTickets` في الديزاين)*
- [ ] قالب الرسالة قابل للتعديل في الإعدادات

**الملفات:** `MRMS.Core/Entities/RepairTicket.cs` · `MRMS.UI/Views/RepairTicketDetailPage.xaml`

#### 4.9 — تحصيل على التذكرة

- [ ] `PaidAmount` على `RepairTicket` + خاصية محسوبة `RemainingAmount`
- [ ] عربون وقت الاستقبال + دفعات جزئية
- [ ] **منع الانتقال لـ `Delivered` والمتبقي > صفر** — إلا بموافقة المالك (`OwnerApprovalDialog` من Phase 2)
- [ ] القيد المحاسبي يتسجّل بالمدفوع الفعلي مش بالتكلفة الكاملة

**الملفات:** `MRMS.Core/Entities/RepairTicket.cs` · `MRMS.Application/Services/RepairTicketService.cs` · `JournalService.cs`

### معيار القبول — Phase 4

- ✅ لوحة Kanban بسبعة أعمدة ملوّنة زي الديزاين
- ✅ محاولة `Received → Delivered` مباشرة → مرفوضة برسالة عربية واضحة
- ✅ إضافة قطعة لتذكرة → الرصيد ينقص فوراً · حذفها → يرجع · دورة `InRepair → WaitingForParts → InRepair` → **الرصيد ما يتغيّرش**
- ✅ حفظ وصف الإصلاح → الحالة **ما تتغيّرش**
- ✅ استقبال جهاز جديد من شاشة واحدة من غير تنقّل
- ✅ محاولة تسليم بمتبقي > صفر → مرفوضة إلا بموافقة المالك

---

# Phase 4-B — مراحل الصيانة الموسّعة *(اختياري — محتاج قرارك)*

**الهدف:** سدّ أكبر مصدرين للخناقات في محلات الصيانة: السعر المفاجئ، والجهاز اللي بيرجع تاني.

> **قرار مطلوب:** المرحلة دي بتضيف حالتين، فالـ Kanban بيبقى ٩ أعمدة بدل ٧ والديزاين هيحتاج تحديث. **مش مطلوبة لتنفيذ الديزاين الحالي.**

- [ ] `AwaitingApproval` بين `UnderInspection` و`WaitingForParts` — سعر تقديري + تسجيل موافقة العميل (مين، إمتى، إزاي) + مسار رفض بـ رسوم فحص
- [ ] `QualityCheck` بين `InRepair` و`Repaired` — تشيك ليست (اتصال، صوت، كاميرا، شحن، شبكة، لمس) + صور «بعد» إجبارية + الرسوب يرجّع التذكرة ويتسجّل كإعادة عمل
- [ ] `Abandoned` — جهاز عدّى المدة على الرف بعد الإبلاغ، بكشف منفصل
- [ ] كشف حالة عند الاستلام: الشاشة، الشاسيه، الملحقات الداخلة، كود القفل، توقيع العميل
- [ ] حجز القطعة من الرصيد في `WaitingForParts` + طلب شراء مربوط بالتذكرة
- [ ] رجوع بالضمان كتذكرة جديدة بتكلفة صفر مربوطة بالأصلية
- [ ] الإرسال لورشة خارجية بتكلفة خارجية تدخل في ربح التذكرة
- [ ] شاشة الأعمار والتأخير عن الميعاد المتوعّد

---

# Phase 5 — درج النقدية والوردية

**الهدف:** صاحب المحل يقدر يتأكد من فلوس الاستقبال كل يوم بضغطة — وده الشيء الوحيد اللي بيمنع التسريب.

**يعتمد على:** Phase 2، Phase 3

> شاشة كاملة في الديزاين (`cashRegister`) ومفيش لها **أي** مقابل في الكود.

### المهام

#### 5.1 — نموذج البيانات

- [ ] `MRMS.Core/Entities/CashSession.cs` (جديد): `Id`, `OpenedByUserId`, `OpenedAt`, `OpeningBalance`, `ClosedByUserId`, `ClosedAt`, `ExpectedAmount`, `CountedAmount`, `Difference`, `Notes`
- [ ] `CashSessionId` على `Sale` و`Expense` و`RepairTicket` (وقت التحصيل) — كل حركة نقدية تنتمي لوردية

#### 5.2 — الخدمة

- [ ] `MRMS.Application/Services/CashSessionService.cs` (جديد)
- [ ] `OpenAsync(openingBalance)` · `GetCurrentAsync()` · `CloseAsync(countedAmount, notes)`
- [ ] `ExpectedAmount = OpeningBalance + المبيعات النقدية + تحصيل الصيانة − المصروفات النقدية`
- [ ] **قفل الوردية بعد الإقفال**: أي حركة بتاريخ وردية مقفولة تبقى للقراءة فقط لغير المالك

#### 5.3 — الشاشة

- [ ] `MRMS.UI/Views/CashRegisterPage.xaml` (جديد) — ٤ بطاقات: الرصيد الافتتاحي · المبيعات · المصروف · **المتوقّع**
- [ ] خانة «النقدية المعدودة» + سطر **الفرق** بلون شرطي: أخضر موجب · أحمر سالب · محايد صفر
- [ ] زر «إقفال اليومية»
- [ ] جدول سجل الإقفالات: التاريخ · الافتتاحي · المتوقّع · المعدود · الفرق

**الملفات:** `MRMS.UI/Views/CashRegisterPage.xaml` (جديد) · `MRMS.UI/ViewModels/CashRegisterViewModel.cs` (جديد)

### معيار القبول — Phase 5

- ✅ فتح وردية → بيع نقدي → صرف مصروف → «المتوقّع» بيتحدّث لحظياً
- ✅ إدخال مبلغ معدود أقل → الفرق أحمر بالقيمة الصحيحة
- ✅ بعد الإقفال، حساب الاستقبال ما يقدرش يعدّل حركة من نفس اليوم
- ✅ الاستقبال بيشوف ورديته هو بس

---

# Phase 6 — فاتورة مبيعات ومديونية العملاء

**الهدف:** العميل ياخد فاتورة بكل اللي اشتراه، والآجل يبقى متتبّع زي آجل الموردين بالظبط.

**يعتمد على:** Phase 2، Phase 5

> `Sale` حالياً = **صنف واحد بكمية**. لو العميل خد جراب وسكرين وشاحن، دي تلات عمليات منفصلة. وعمود `debt` في جدول العملاء بتاع الديزاين مالوش مقابل في الكود.

### المهام

#### 6.1 — فاتورة متعددة البنود

- [ ] `MRMS.Core/Entities/SalesInvoice.cs` (جديد): `InvoiceNumber`, `Date`, `CustomerId?`, `CustomerName`, `Subtotal`, `DiscountAmount`, `Total`, `PaidAmount`, `PaymentMethod`, `CashSessionId`, `UserId`
- [ ] `MRMS.Core/Entities/SalesInvoiceLine.cs` (جديد): `ItemId`, `Quantity`, `UnitPrice`, `UnitCost`, `LineTotal`
- [ ] ترحيل `Sale` الحالي لفواتير ببند واحد — **من غير فقدان بيانات**
- [ ] `UnitCost` بيتقفل من `Item.CostPrice` لحظة البيع (مش قابل للتعديل اليدوي) — ده بيصلح حساب الربح

#### 6.2 — ربط بجدول العملاء

- [ ] `CustomerId` اختياري على الفاتورة، مع إبقاء `CustomerName` للبيع السريع
- [ ] `CustomerBalance` محسوب = مجموع الفواتير الآجلة − التحصيلات
- [ ] `MRMS.Core/Entities/CustomerPayment.cs` (جديد) — تحصيل من عميل
- [ ] **عمود «مديونية» في جدول العملاء** بلون `#B45309` لو > صفر *(مطابق للديزاين)*
- [ ] كشف حساب عميل في ملفه الشخصي

#### 6.3 — الشاشة والطباعة

- [ ] `MRMS.UI/Views/SaleFormPage.xaml` — جدول بنود + زر «+ إضافة سطر» + خصم + إجمالي
- [ ] الربح ظاهر للمالك فقط (Phase 2.4)
- [ ] طباعة فاتورة بترويسة المحل
- [ ] مرتجع على فاتورة (يرجّع الرصيد ويعكس القيد)

#### 6.4 — الباركود

- [ ] قراءة سكانر في خانة الصنف (الاستماع لإدخال سريع منتهي بـ Enter)
- [ ] طباعة ليبل باركود من شاشة الأصناف

**الملفات:** `MRMS.Core/Entities/` · `MRMS.Application/Services/SaleService.cs` · `MRMS.UI/Views/SaleFormPage.xaml` · `SalesPage.xaml` · `CustomerProfilePage.xaml`

### معيار القبول — Phase 6

- ✅ فاتورة بثلاث بنود برقم واحد وطباعة واحدة
- ✅ بيع آجل لعميل → المديونية تظهر في جدول العملاء
- ✅ تحصيل من العميل → المديونية تقل
- ✅ مسح باركود → الصنف يتضاف للفاتورة فوراً

---

# Phase 7 — المخزون والتنبيهات

**الهدف:** ما يحصلش إن العميل يستنى قطعة والقطعة خلصت من أسبوع ومحدش واخد باله.

**يعتمد على:** Phase 3

### المهام

- [ ] **نوع صنف ثالث: `Service`** — رصيد `null` ومش داخل في حساب قيمة المخزون *(زي «خدمة تركيب شاشة» في الديزاين)*
- [ ] ٣ بطاقات إحصائية أعلى شاشة المخزون: **قيمة المخزون** (للمالك فقط) · عدد الأصناف المنخفضة · عدد الأصناف النافدة
- [ ] وسم حالة على كل صف: `متوفر` أخضر · `منخفض` أصفر · `نفذ من المخزون` أحمر · `—` للخدمات
- [ ] `MRMS.UI/Views/StockCountPage.xaml` (جديد) — جرد وتسوية: الرصيد الدفتري مقابل الفعلي، الفرق يتسجّل كحركة تسوية موثّقة باسم المستخدم
- [ ] منع الرصيد السالب في كل مسارات الخصم
- [ ] تقرير «الأصناف اللي محتاجة طلب» جاهز للتحويل لأمر شراء

**الملفات:** `MRMS.Core/Enums/ItemType.cs` · `MRMS.UI/Views/ItemsPage.xaml` · `StockCountPage.xaml` (جديد) · `MRMS.Application/Services/ItemService.cs`

### معيار القبول — Phase 7

- ✅ صنف نوعه «خدمة» ما لهوش رصيد وما بيظهرش في تنبيهات النقص
- ✅ رصيد يوصل لحد الطلب → وسم أصفر + تنبيه في الجرس
- ✅ جرد بفرق → حركة تسوية باسم المستخدم في سجل التدقيق

---

# Phase 8 — التقارير والتصدير

**الهدف:** المالك يقفل الشهر في ٥ دقايق، والمحاسب الخارجي يستلم ملف.

**يعتمد على:** Phase 2

### المهام

- [ ] **توحيد صيغ الحساب**: `salesRevenue + repairRevenue`, `cogs`, `grossProfit` متكرّرة حرفياً في `IncomeStatementService` و`MonthlyReportService` و`ReportService` و`DashboardService` → استخراجها في `MRMS.Application/Reporting/FinancialCalculator.cs` (جديد)
- [ ] نفس الحاجة لحساب المستحق للموردين — مكرّر في ٣ أماكن
- [ ] شاشة تقارير موحّدة بتبديل **هذا الشهر / الشهر الماضي** *(زي `period` في الديزاين)*
- [ ] قائمة الدخل بالشكل بتاع الديزاين: إجمالي المبيعات · (تكلفة البضاعة) · مجمل الربح · (المصروفات) · **صافي الربح** بلون `#16A34A` ووزن `700`
- [ ] تقارير جديدة: ربحية الفنيين · أكثر الأصناف مبيعاً · أعمار المديونيات · حركة صنف
- [ ] **تصدير Excel و PDF** لكل تقرير
- [ ] `AuditLogPage` بفلترة كاملة

**الملفات:** `MRMS.Application/Reporting/` (جديد) · `MRMS.UI/Views/*ReportPage.xaml`

### معيار القبول — Phase 8

- ✅ نفس رقم صافي الربح في لوحة التحكم وقائمة الدخل والتقرير الشهري
- ✅ تصدير أي تقرير لـ Excel بأرقام مطابقة للشاشة

---

# Phase 9 — التجهيز للبيع كمنتج

**الهدف:** حد يشتري النظام ويركّبه بنفسه من غير ما تروح له.

**يعتمد على:** كل المراحل

### المهام

- [ ] **معالج التهيئة أول تشغيل**: بيانات المحل (اسم، لوجو، تليفون، عنوان) · حساب المالك · الأدوار · الأصناف الافتتاحية · الرصيد النقدي الافتتاحي
- [ ] الترويسة دي تظهر في كل طباعة (إيصال، فاتورة، كارت ضمان)
- [ ] **Installer** (`WiX` أو `Inno Setup`) بدل مجلد `MRMS_Release`
- [ ] نظام ترخيص/تفعيل
- [ ] نسخ احتياطي مجدول تلقائي (`BackupSchedulerService` موجود — يتفعّل ويتوثّق)
- [ ] دليل مستخدم عربي مختصر + فيديو تعريفي
- [ ] فروع ومخازن متعددة *(لو السوق طلبها)*
- [ ] الفاتورة الإلكترونية لو العميل مسجّل ضريبياً

---

## خريطة الشاشات ↔ الملفات

| شاشة الديزاين | ملف الواجهة | الحالة |
|---|---|---|
| `dashboard` | `Views/DashboardPage.xaml` | تعديل — Phase 3.4 |
| `tickets` (Kanban) | `Views/RepairTicketListPage.xaml` | تعديل كبير — Phase 4.1 |
| `ticketDetail` | `Views/RepairTicketDetailPage.xaml` | تعديل — Phase 4.5/4.7/4.8/4.9 |
| `intake` | `Views/IntakePage.xaml` | **جديد** — Phase 4.6 |
| `transfer` | `Views/DeviceTransferPage.xaml` | إعادة تنسيق — Phase 1.5 |
| `customers` | `Views/CustomerListPage.xaml` | تعديل — Phase 6.2 |
| `sales` | `Views/SaleFormPage.xaml` + `SalesPage.xaml` | تعديل كبير — Phase 6 |
| `purchases` | `Views/PurchaseFormPage.xaml` + `PurchasesPage.xaml` | إعادة تنسيق — Phase 1.5 |
| `inventory` | `Views/ItemsPage.xaml` | تعديل — Phase 7 |
| `suppliers` | `Views/SuppliersPage.xaml` | تعديل — Phase 2.4 |
| `cashRegister` | `Views/CashRegisterPage.xaml` | **جديد** — Phase 5.3 |
| `supplierPayments` | `Views/SupplierPaymentsPage.xaml` | تعديل — Phase 2.3 |
| `expenses` | `Views/ExpensesPage.xaml` | تعديل — Phase 2.3 |
| `reports` | `Views/IncomeStatementPage.xaml` + `MonthlyReportPage.xaml` + `CashbookPage.xaml` | توحيد — Phase 8 |
| `search` | `Views/SearchPage.xaml` | تعديل — Phase 3.2 |
| `settings` | `Views/SettingsPage.xaml` | تعديل — Phase 2.3 |
| — | `Views/UsersPage.xaml` | **جديد** — Phase 2.7 |
| — | `Views/AuditLogPage.xaml` | **جديد** — Phase 2.7 |
| — | `Views/StockCountPage.xaml` | **جديد** — Phase 7 |
| — | `Views/OwnerApprovalDialog.xaml` | **جديد** — Phase 2.5 |

---

## المخاطر

| الخطر | الأثر | التصرّف |
|---|---|---|
| **البيانات الموجودة منحرفة** — الأصناف والتذاكر فيها خصم ناقص من قبل | أرقام المخزون غلط بعد الترحيل | جرد كامل مرة واحدة قبل التشغيل بالنسخة الجديدة (Phase 7) |
| **الترحيل من `EnsureCreated` للـ Migrations** على قاعدة موجودة | فشل التشغيل عند العميل | Phase 0.3 + نسخة احتياطية إجبارية قبل أول تشغيل |
| **ترحيل `Sale` لفواتير** | فقدان تاريخ المبيعات | ترحيل يحوّل كل صف لفاتورة ببند واحد، مع الاحتفاظ بالجدول القديم فترة |
| **إعادة تهشيم الباسوردات** | قفل المستخدمين برّه | ترحيل عند أول دخول ناجح، والقديم يفضل شغّال لحد ما يتحوّل |
| **حجم Phase 2** | تعطّل باقي الشغل | تتنفّذ على دفعتين: `2.1–2.3` (النواة) ثم `2.4–2.7` (الحجب والشاشات) |
| **Kanban على بيانات كبيرة** | بطء | تحميل كسول لكل عمود + حد أقصى للصفوف الظاهرة |

---

## ملخص في أرقام

| | |
|---|---|
| عدد المراحل | **10** (منها واحدة اختيارية) |
| كيانات جديدة | `AuditLog` · `CashSession` · `SalesInvoice` · `SalesInvoiceLine` · `CustomerPayment` — **5** |
| شاشات جديدة | `IntakePage` · `CashRegisterPage` · `UsersPage` · `AuditLogPage` · `StockCountPage` · `OwnerApprovalDialog` — **6** |
| خدمات جديدة | `PermissionService` · `CurrentUserService` · `AlertService` · `CashSessionService` · `FinancialCalculator` — **5** |
| أعطال حرجة تُصلَح في Phase 0 | **6** |

---

**الخطوة التالية:** ابدأ بـ **Phase 0** — من غيرها أي شغل بعدها هيتبني على قاعدة بتفقد البيانات وما بتترحّلش.
