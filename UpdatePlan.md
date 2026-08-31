# Update Plan — Purchases/Payments/Expenses UX + Journal Cleanup + QR Scanning

**Source:** developer request (chat), 2026-08-14. Each task below is written to be executed **independently** — its own files, its own build/verify step — so any one of them can be picked up, implemented, and checked off without depending on the others being done first (dependencies are called out explicitly where they exist).

**Convention:** one task = one unit of work. Do not batch two tasks into a single edit pass; verify build after each task before moving to the next.

---

## T1 — Purchase invoice detail view (click a purchase → see everything as entered)

**Request (AR):** "لما أضغط على فاتورة المشتريات, الفاتورة تظهرلي بكل حاجة فيها زي ما دخلتها"

**Current state:** [PurchasesPage.xaml](MRMS.UI/Views/PurchasesPage.xaml) lists purchases in a `DataGrid` with a "حذف" (delete) button per row. There is no click/double-click handler on a row, and no detail view exists anywhere — `PurchaseService` has `GetByIdAsync` (returns full `PurchaseDto` with line items) but nothing in the UI calls it outside of `PurchaseFormViewModel.SaveAsync`'s return value.

**Why not reuse `PurchaseFormPage`:** that page's `PurchaseFormViewModel` is `Transient` and wired for *creating* a new invoice (no "load existing purchase" path, no edit-mode concept, and `PurchaseService.UpdateAsync` doesn't touch line items — see [AUDIT.md §5](AUDIT.md) for why). Retrofitting it for view/edit is a separate, bigger change than what was asked (which is just "show it to me as I entered it"). A dedicated read-only detail surface is the right scope for this request.

**Plan:**
1. Add `PurchaseDetailWindow.xaml` + `.xaml.cs` (a small modal `Window`, not a `Page` — avoids the Frame/back-stack losing state, and matches the existing modal pattern already used by `PrintPreviewWindow.xaml`).
2. Window shows: date, supplier name, payment method, paid amount, total cost, notes, and a read-only `DataGrid` of line items (item name, quantity, unit cost, line total) — i.e. every field `PurchaseDto`/`PurchaseItemDto` already carries.
3. Constructor takes `IPurchaseService` + the purchase `Id`; on `Loaded`, calls `GetByIdAsync(id)` and binds the result (no ViewModel needed for a read-only screen — direct `DataContext = dto` is enough, or a tiny wrapper if formatting requires it).
4. In [PurchasesPage.xaml](MRMS.UI/Views/PurchasesPage.xaml), add `MouseDoubleClick` on the `DataGrid` (row-level, ignoring clicks on the delete button) → resolve `PurchaseDetailWindow` via `App.ServiceProvider`, pass the selected row's `Id`, `.ShowDialog()`.
5. Register `PurchaseDetailWindow` as `Transient` in [App.xaml.cs](MRMS.UI/App.xaml.cs).

**Files touched:** new `MRMS.UI/Views/PurchaseDetailWindow.xaml(.cs)`; edit `PurchasesPage.xaml(.cs)`; edit `App.xaml.cs` (DI registration).

**Verify:** `dotnet build MRMS.UI/MRMS.UI.csproj`; manually confirm double-clicking a purchase row (not the delete button) opens the modal with all fields matching what's in the grid/DB.

---

## T2 — Auto-fill purchase item cost price from the Items table + show current stock

**Request (AR):** "أنا مجرد ما بختار الصنف وأحط الكمية بتاعته، سعر الشراء يتجاب من جدول الأصناف نفسه... ويحط في... ويجيب الكمية الكلية بتاعته"

**Current state:** [PurchaseFormViewModel.cs](MRMS.UI/ViewModels/PurchaseFormViewModel.cs) `AddItem()` only falls back to `item.CostPrice` if `SelectedUnitCost` is still `0` at the moment "إضافة" is clicked (`SelectedUnitCost > 0 ? SelectedUnitCost : item.CostPrice`). There is no reactive fill the instant the item is picked (unlike `SaleFormViewModel.OnSelectedItemIdChanged`, which does exactly this for sales), and nothing displays the item's current total stock next to the picker.

**Plan:**
1. Add `partial void OnSelectedItemIdChanged(int value)` to `PurchaseFormViewModel`, mirroring `SaleFormViewModel`: look up the item in `Items`, set `SelectedUnitCost = item.CostPrice`, and set a new `[ObservableProperty] SelectedItemCurrentStock` (int) from `item.CurrentStock`.
2. In [PurchaseFormPage.xaml](MRMS.UI/Views/PurchaseFormPage.xaml), next to the item `ComboBox`/quantity/cost row, add a small `TextBlock` bound to `SelectedItemCurrentStock` (e.g. "المتوفر حالياً: {N}") so the person entering the invoice can see current stock before adding more.
3. Keep the field editable after auto-fill (cost can still be corrected per-invoice, since purchase cost legitimately varies invoice to invoice — don't lock it read-only).

**Files touched:** `PurchaseFormViewModel.cs`, `PurchaseFormPage.xaml`.

**Verify:** build; manually confirm picking an item fills the cost box immediately (not just on "إضافة") and shows current stock.

---

## T3 — "Add new item" quick button inside the purchase form

**Request (AR):** "لو في صنف جديد مش عندي... زرار... يفتح شاشة إضافة الصنف... وبعدين يظهر لي في فاتورة المشتريات كأنه صنف موجود بالضبط"

**Current state:** Adding an item today means leaving the purchase form entirely (`ItemsPage` → `ItemFormPage`, both `Transient`, navigated via the shared `Frame`), which — per `AppNavigationService.NavigateTo` — discards the in-progress `PurchaseFormViewModel` (a fresh instance is created on `GoBack`/re-navigate), losing every line already added to the invoice. That's the actual bug behind why this request exists, not just a missing button.

**Depends on:** none (independent of T2, but naturally sits next to it in the same XAML — implement T2 first to avoid a merge conflict in `PurchaseFormPage.xaml`, not a hard requirement).

**Plan:**
1. Add `QuickAddItemWindow.xaml(.cs)` — a modal `Window` hosting the *existing* `ItemFormViewModel`/its form fields (code, name, type, unit, cost price, selling price, reorder level), reusing `ItemFormViewModel.SaveAsync` as-is (it already returns via `_navigationService.GoBack()` on success inside a `Page`-oriented VM — for the modal window, add a `event Action<ItemDto>? ItemSaved` to `ItemFormViewModel` fired after a successful create, instead of relying on `GoBack()`; guard it so `GoBack()` is only called when a `_navigationService` frame context actually exists, or simplest: give the modal its own small VM member that just calls `IItemService.CreateAsync` directly and raises the event — do not fight the Page-oriented `GoBack()` call in a modal context).
2. Simplest concrete approach: `QuickAddItemWindow` gets its own thin code-behind that talks to `IItemService.CreateAsync` directly (same validation copy as `ItemFormViewModel.SaveAsync` for code/name-required), *not* reusing `ItemFormViewModel` — avoids the `GoBack()` conflict above and keeps this task's blast radius small. Window closes with `DialogResult = true` and exposes the created `ItemDto` as a property on success.
3. In [PurchaseFormPage.xaml](MRMS.UI/Views/PurchaseFormPage.xaml), add a "+ صنف جديد" button next to the item `ComboBox`. Click handler: open `QuickAddItemWindow` via `.ShowDialog()`; if `DialogResult == true`, re-run `LoadCommand`'s item-loading (or just append the new item to `PurchaseFormViewModel.Items` directly) and set `SelectedItemId` to the new item's `Id` — so it now behaves exactly like any pre-existing item (T2's auto-fill fires immediately since `SelectedItemId` changed).

**Files touched:** new `MRMS.UI/Views/QuickAddItemWindow.xaml(.cs)`; edit `PurchaseFormPage.xaml(.cs)`, `PurchaseFormViewModel.cs` (expose a method to append+select an item without a full reload); `App.xaml.cs` (DI registration).

**Verify:** build; manually confirm adding a new item mid-invoice does not clear already-added purchase lines, and the new item appears selected/priced correctly right after.

---

## T4 — Fix supplier payments delete-button layout

**Request (AR):** "في شاشة دفعات الموردين في زرار الحذف كده والlayout مش مضبوطة... جزء منها كده مش واضح"

**Root cause:** [SupplierPaymentsPage.xaml:48](MRMS.UI/Views/SupplierPaymentsPage.xaml#L48) uses `<DataGridTemplateColumn Header="حذف" Width="60">` for the delete button — every other page using the same `DangerActionButton` style (`PurchasesPage`, `ExpensesPage`) uses `Width="80"` for that column. At `Width="60"`, the button (padding `12,6` + border + "حذف" text) is too tight for the column, which is the "not clear" layout the user is describing.

**Plan:**
1. Widen the delete column from `60` to `80` (matching `PurchasesPage`/`ExpensesPage`), and rename the header from `"حذف"` to `"الإجراءات"` for consistency with those two pages (the button itself still says "حذف").
2. Explicitly set `HorizontalAlignment="Center"` on the `Button` inside the cell template (not currently set anywhere, including in the two "working" pages — cheap insurance against the same issue recurring).

**Files touched:** `SupplierPaymentsPage.xaml` only.

**Verify:** build; visually confirm the delete button renders fully inside its column with clear spacing at the default window width (1100px per `MainWindow.xaml`).

---

## T5 — Expenses page: month filter + running total

**Request (AR):** "عايز فلتر... مصروفات الشهر ده... الشهر اللي فات... وحاجة سريعة تبينلي إجمالي المصروفات كام"

**Current state:** [ExpensesViewModel.cs](MRMS.UI/ViewModels/ExpensesViewModel.cs) already has a text `SearchQuery` filter (category/description) and pagination, but no date/month filter and no total display. [ExpensesPage.xaml](MRMS.UI/Views/ExpensesPage.xaml) only shows `Expenses.Count`.

**Plan:**
1. Add a `PeriodFilter` enum (or simple string set) to `ExpensesViewModel`: `All`, `ThisMonth`, `LastMonth` — exposed as an `[ObservableProperty]` bound to a `ComboBox`/segmented buttons in the page header ("الكل" / "هذا الشهر" / "الشهر الماضي").
2. Extend the existing `_filteredExpenses` computed property to additionally filter by the selected period (`e.Date.Year/Month` matched against `DateTime.Today`'s month, or the previous month) — compose with the existing text-search filter, not replace it.
3. Add a computed `FilteredTotal => _filteredExpenses.Sum(e => e.Amount)` (over the *filtered* set, not just the current page) and bind it in `ExpensesPage.xaml` next to the existing "إجمالي المصروفات" count — e.g. "الإجمالي: {N0}" — so it updates live as the filter changes.
4. Re-run `ApplyPaging()` (and raise `OnPropertyChanged(nameof(FilteredTotal))`) whenever `PeriodFilter` or `SearchQuery` changes, and reset `CurrentPage = 1` on filter change (same pattern already used for `SearchQuery`).

**Files touched:** `ExpensesViewModel.cs`, `ExpensesPage.xaml`.

**Verify:** build; manually confirm switching "هذا الشهر"/"الشهر الماضي"/"الكل" changes both the grid contents and the total figure correctly against the known seed/live data.

---

## T6 — Remove the Journal Entries screen ("قيود اليومية")

**Request (AR):** "شيل شاشة الكود اليومية دي مالهاش أي لازمة"

**Context:** per [AUDIT.md §5.2](AUDIT.md), this screen has always shown empty data in practice (the journal-posting subsystem never persists), so removing it loses nothing that was ever working.

**Plan:**
1. Remove the "قيود اليومية" button and its `Click="JournalEntries_Click"` handler from [HomePage.xaml](MRMS.UI/Views/HomePage.xaml) / [HomePage.xaml.cs](MRMS.UI/Views/HomePage.xaml.cs).
2. Remove `services.AddTransient<JournalEntriesViewModel>();` and `services.AddTransient<JournalEntriesPage>();` from [App.xaml.cs](MRMS.UI/App.xaml.cs).
3. Delete `MRMS.UI/Views/JournalEntriesPage.xaml`, `JournalEntriesPage.xaml.cs`, `MRMS.UI/ViewModels/JournalEntriesViewModel.cs`.
4. **Explicitly out of scope for this task:** `IJournalService`/`JournalService` and the `JournalEntries`/`JournalEntryLines` tables stay — they're called from six other services (`SaleService`, `PurchaseService`, `ExpenseService`, `SupplierPaymentService`, `StockMovementService`, `RepairTicketService`) and removing that backend is a separate, larger change than "remove this screen." (Worth a future decision — see note at the end of this plan.)

**Files touched:** delete 3 files; edit `HomePage.xaml(.cs)`, `App.xaml.cs`.

**Verify:** build; confirm the nav sidebar no longer shows "قيود اليومية" and the app still starts/navigates normally.

---

## T7 — Remove the Trial Balance screen ("ميزان المراجعة")

**Request (AR):** "ميزان المراجعة ده مالوش أي لازمة"

Same rationale and dependency-free removal as T6, applied to `TrialBalancePage`/`TrialBalanceViewModel`.

**Plan:**
1. Remove the "ميزان المراجعة" button and `Click="TrialBalance_Click"` handler from `HomePage.xaml` / `HomePage.xaml.cs`.
2. Remove its two `AddTransient` DI registrations from `App.xaml.cs`.
3. Delete `MRMS.UI/Views/TrialBalancePage.xaml`, `TrialBalancePage.xaml.cs`, `MRMS.UI/ViewModels/TrialBalanceViewModel.cs`.
4. `JournalService.GetTrialBalanceAsync` (the Application-layer method) stays for the same reason as T6 #4.

**Files touched:** delete 3 files; edit `HomePage.xaml(.cs)`, `App.xaml.cs`.

**Verify:** build; confirm nav no longer shows "ميزان المراجعة".

---

## T8 — QR/barcode scan support in Sales

**Request (AR):** "لازم السيستم يدعم QR reader... في المبيعات، بدل ما أكتب اسم الصنف، أنا هامسح الصنف بالـ QR... ويظهر لي على طول كأني عملت له بيع"

**How this actually works technically:** a QR/barcode "reader" of the kind implied here (USB/Bluetooth handheld scanner) is a **keyboard-wedge device** — it has no app-level API; it just "types" the decoded text into whatever text field currently has focus, then sends an `Enter` keystroke. So "supporting a QR reader" = giving the relevant screen a text field that's focused and ready, and reacting to `Enter`/text-changed the way a human typing-then-pressing-Enter would. No new hardware integration/SDK is required.

**Current state:** [SaleFormPage.xaml](MRMS.UI/Views/SaleFormPage.xaml) only has a `ComboBox` for item selection — no code-entry field at all. `Item.Code` (unique, per [AUDIT.md §3.2](AUDIT.md)) is the natural value to scan against — it's exactly the kind of value a shop would print as a barcode/QR label on stock.

**Plan:**
1. Add `IItemService.GetByCodeAsync(string code)` (exact match, not the existing `Contains`-based `SearchAsync`) to `IItemService`/`ItemService` — a barcode scan must resolve to exactly one item or "not found," never a fuzzy list.
2. Add a "مسح الباركود" `TextBox` at the top of `SaleFormPage`, above the item `ComboBox`, bound to a new `ScanCode` property on `SaleFormViewModel`, with `KeyDown` wired (in code-behind, same pattern as `DeviceRegistrationPage.Imei_KeyDown`) to a new `ScanItemCommand` on Enter.
3. `ScanItemCommand`: call `GetByCodeAsync(ScanCode)`; on match, set `SelectedItemId` (which already triggers the existing cost/price auto-fill), clear `ScanCode`, and move focus to the `Quantity` field so the flow is "scan → confirm qty → save" with no mouse needed; on no match, show an inline error ("لم يتم العثور على صنف بهذا الكود") via the existing `ErrorMessage` property.
4. Auto-focus the scan field on page `Loaded` (mirrors T10) so a scan works the instant the Sales form opens.

**Files touched:** `IItemService.cs`, `ItemService.cs`, `SaleFormViewModel.cs`, `SaleFormPage.xaml(.cs)`.

**Verify:** build; manually confirm typing a known `Item.Code` + Enter into the new field selects the same item as picking it from the dropdown, and an unknown code shows a clear error instead of throwing.

---

## T9 — QR/barcode scan support in the Purchases item picker

**Request (AR):** "أي كود لبروڊكت معين همسحه بالجهاز" (extends the scanning request beyond Sales to product codes generally)

**Depends on:** T2 (reactive cost auto-fill) and ideally T8 (reuses the same `IItemService.GetByCodeAsync` added there) — implement after both if possible, but not strictly blocked if done first (just add `GetByCodeAsync` here instead if T8 hasn't landed yet).

**Plan:**
1. Add the same "مسح الباركود" `TextBox` pattern to `PurchaseFormPage`'s item-entry row (next to the item `ComboBox`/qty/cost/إضافة controls).
2. On Enter: resolve via `IItemService.GetByCodeAsync`, set `SelectedItemId` (fires T2's auto-fill of `SelectedUnitCost` + stock display), and focus the quantity box — scanning does **not** auto-add the line (unlike Sales' single-item flow, a purchase invoice is multi-line, so quantity must still be confirmed/entered before "إضافة"/pressing the add button, otherwise a mis-scan silently adds a wrong-quantity line).
3. Same "not found" inline error handling as T8.

**Files touched:** `PurchaseFormViewModel.cs`, `PurchaseFormPage.xaml(.cs)`.

**Verify:** build; confirm scanning a known item code selects+prices it in the purchase item row without adding a line until quantity is confirmed and "إضافة" is pressed.

---

## T10 — Verify/harden IMEI fields for scan-ready use

**Request (AR):** "أي IMEI أمسحه بالجهاز بدل ما أكتبه"

**Current state:** [DeviceRegistrationPage.xaml.cs:29-34](MRMS.UI/Views/DeviceRegistrationPage.xaml.cs#L29-L34) already wires `Enter` on the IMEI box to `SearchImeiCommand` — since a scanner sends the code + `Enter`, this **already works** as scan input today, mechanically. What's missing is making it *frictionless*: the field isn't auto-focused when the page opens, so a tech would still need to click into the box once before the first scan of a session.

**Plan:**
1. `DeviceRegistrationPage`: focus the IMEI `TextBox` on `Loaded`, and re-focus it after a search completes (whether found or not) and after `ShowSearchImeiStep` becomes active again (e.g. after finishing one intake, ready for the next scan immediately).
2. `DeviceTransferPage`/`DeviceTransferViewModel`: same check — confirm its IMEI box has the equivalent `KeyDown`→`FindDeviceCommand` wiring (needs a quick read of `DeviceTransferPage.xaml`/`.xaml.cs`, not yet confirmed) and add it + auto-focus if missing.
3. No backend change needed — this task is purely "make the existing Enter-triggered search convenient for a scanner," not new scanning logic.

**Files touched:** `DeviceRegistrationPage.xaml.cs`, and `DeviceTransferPage.xaml(.cs)` if it turns out to lack the Enter-key wiring.

**Verify:** build; manually confirm the IMEI field has focus the moment each relevant page loads and after each search, with no click required.

---

## Suggested execution order

Independent, can be done in any order: **T4, T5, T6, T7** (pure UI/cleanup, smallest blast radius).
Do next, same feature area: **T2 → T3** (T3 benefits from T2's `OnSelectedItemIdChanged` already existing, avoids touching the same method twice).
Then: **T1** (independent, but naturally grouped with the Purchases work above).
Then: **T8 → T9 → T10** (T9 reuses T8's `GetByCodeAsync`; T10 is unrelated but small, good to close out the "QR support" ask together).

## Open item not in scope above

T6/T7 remove the *screens* only, per the explicit request. The underlying journal-posting bug documented in [AUDIT.md §5.2](AUDIT.md) (journal entries silently never persist, root-caused to the `Transient` `AppDbContext`/`IUnitOfWork` DI lifetime) is untouched by this plan. If the intent is to abandon double-entry bookkeeping entirely, `IJournalService`/`JournalService` and its six call sites could be removed too in a follow-up task — flagging this rather than assuming it, since it's a bigger change than what was asked for today.
