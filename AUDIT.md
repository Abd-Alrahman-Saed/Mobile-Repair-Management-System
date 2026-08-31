# MRMS — Codebase Audit

**Scope:** Full solution audit (read-only). No source files were modified.
**Method:** Static reading of every project/service/viewmodel, plus live inspection of the actual SQL Server database (`ABDO-DESKTOP\SQLEXPRESS` / `MRMS`) and a real `dotnet build` of every project to verify claims empirically rather than by inference.
**Date of audit:** 2026-08-14

---

## 1. Executive Summary

MRMS is a WPF desktop application for a mobile-phone repair shop, covering two domains: (1) repair-ticket lifecycle management (customer, device, ticket, parts, photos, warranty) and (2) an accounting/POS layer (items, suppliers, purchases, sales, expenses, stock movements, supplier payables, journal entries, trial balance, income statement, monthly reports). It is built on .NET 10 / WPF with a clean-looking layered architecture (Core → Application → Infrastructure → UI), EF Core 10 against SQL Server, CommunityToolkit.Mvvm for MVVM, and Serilog for logging. The repair-ticket module (the older, more mature half of the app) is functionally coherent. The accounting module was bolted on afterward, in the developer's own words, to "evolve MRMS from a repair-ticket manager into a production-ready accounting system" (see [ImplementationPlan.md](ImplementationPlan.md)), and it shows: it is functionally broad but structurally incomplete, and it contains the single biggest defect found in this audit.

**The single biggest risk:** the entire double-entry journal/accounting subsystem (`JournalService`, Trial Balance, and by extension any report that depends on posted journal entries) is silently non-functional in the running application. This was not inferred from reading code — it was confirmed against the live database: `Sales`, `Purchases`, `Expenses`, and `SupplierPayments` all contain rows created *after* the journal feature (Phase 7) shipped, yet `JournalEntries` contains **zero** rows. The root cause is a dependency-injection lifetime bug (§7, §5) — every service, including `AppDbContext` itself, is registered `Transient`, so a service and the `IJournalService` it calls internally each get their own, different `DbContext` instance. The journal-posting calls succeed silently (no exception, nothing logged) but write to a change-tracker that is never persisted. Every downstream report built on journal data (Journal Entries page, Trial Balance page) will always show empty/zero results in production, even though the code "looks" correct on a read-through.

**Maturity assessment:** this is a solid mid-development prototype, not yet production-ready. The repair-ticket core is close. The accounting core has the journal defect above, plus several data-integrity gaps around stock concurrency, non-atomic multi-step writes, an unvalidated ticket-status state machine that already shows real drift in the live data (see §4), and a test suite that currently **does not compile** (`dotnet build` on `MRMS.Tests` fails with 4 errors — confirmed empirically, not assumed). There is no configuration/secrets management (SQL Server connection string, including a real machine name, is hardcoded in two source files), no applied EF Core migration strategy in the running seeding path, and no deployment/backup story documented anywhere.

---

## 2. Architecture Overview

### 2.1 Solution/project structure

| Project | TargetFramework | Role |
|---|---|---|
| [MRMS.Core](MRMS.Core/MRMS.Core.csproj) | net10.0 | Domain entities, enums, `IRepository<T>` / `IUnitOfWork` abstractions. No dependencies on anything else. |
| [MRMS.Application](MRMS.Application/MRMS.Application.csproj) | net10.0 | Service layer: one interface + implementation per feature area, DTOs (mostly C# `record`s), `DependencyInjection.cs` registrar. References Core and (directly) `Microsoft.EntityFrameworkCore` — see §2.3. |
| [MRMS.Infrastructure](MRMS.Infrastructure/MRMS.Infrastructure.csproj) | net10.0 | `AppDbContext` (EF Core + SQL Server), generic `Repository<T>`, `UnitOfWork`, `SeedService`, Serilog packages, EF migrations. |
| [MRMS.UI](MRMS.UI/MRMS.UI.csproj) | net10.0-windows, WPF | Views (XAML + code-behind), ViewModels (CommunityToolkit.Mvvm), `App.xaml.cs` composition root, navigation/notification services, converters, LiveChartsCore for the monthly report. |
| [MRMS.Tests](MRMS.Tests/MRMS.Tests.csproj) | net10.0 | xUnit + Moq + FluentAssertions unit tests for 4 of ~20 services. **Currently does not compile** (§7). |

Dependency direction is correctly one-way (`UI → Infrastructure → Application → Core`, with `Infrastructure` also depending on `Application` for `SeedService`'s access to entities). There is no separate `MRMS.WebApi` or similar — this is a single desktop executable talking directly to SQL Server, no network service boundary.

### 2.2 Architectural pattern in use

This is a **layered architecture with a thin repository/unit-of-work layer**, not full Clean Architecture / CQRS / mediator-based. Concretely:

- **UI layer**: WPF Views + code-behind + ViewModels (MVVM via `CommunityToolkit.Mvvm`'s `[ObservableProperty]`/`[RelayCommand]` source generators). Code-behind is mostly thin (event wiring, file dialogs, navigation), but there are real exceptions — `RepairTicketDetailPage.xaml.cs` reaches into `App.ServiceProvider!.GetRequiredService<IRepairTicketService>()` directly (a **service-locator anti-pattern**, bypassing constructor injection) rather than having the page's own ticket-loading logic live in a ViewModel command. See [RepairTicketDetailPage.xaml.cs:49-51](MRMS.UI/Views/RepairTicketDetailPage.xaml.cs#L49-L51).
- **Application layer**: one service class per aggregate (`SaleService`, `PurchaseService`, `RepairTicketService`, etc.), each taking `IUnitOfWork` (and sometimes `IJournalService`) via constructor injection. Business rules (stock checks, validation messages in Arabic) live here, which is good — they are not duplicated in the UI layer for the write path. However, some duplication exists on the *display/read* side (§7.4).
- **No CQRS/MediatR, no domain events, no domain-layer business rules** — `MRMS.Core` entities are anemic POCOs; all logic is in `MRMS.Application` services directly.
- **No API/backend boundary** — the desktop app is a monolith that owns the SQL Server connection directly. This means the "cross-cutting" concerns (auth, validation, transactions) that would normally live at an API gateway are the sole responsibility of each Application-layer service, and consistency is not centrally enforced (see §7.3).

### 2.3 Data access approach

**EF Core 10** (`Microsoft.EntityFrameworkCore.SqlServer`), accessed exclusively through a **generic repository + unit-of-work** wrapper ([Repository.cs](MRMS.Infrastructure/Repositories/Repository.cs), [UnitOfWork.cs](MRMS.Infrastructure/Repositories/UnitOfWork.cs)) that mostly just proxies to `DbSet<T>`/`SaveChangesAsync`. `Repository<T>.Query()` exposes the raw `IQueryable<T>`, so most services bypass the repository's `FindAsync` and write LINQ directly against `.Query()` with `.Include()` — meaning the "repository pattern" here adds an indirection layer without actually encapsulating query logic; it does not prevent leaking IQueryable/EF specifics into the Application layer. Given this, and given `MRMS.Application.csproj` directly references `Microsoft.EntityFrameworkCore` ([MRMS.Application.csproj:8](MRMS.Application/MRMS.Application.csproj#L8)) and multiple services call `.Include()`/`.ThenInclude()` directly (e.g. [PurchaseService.cs:19-23](MRMS.Application/Services/PurchaseService.cs#L19-L23)), the repository abstraction is not actually decoupling the Application layer from EF Core — it's a thin, mostly redundant wrapper.

There is no Dapper, no raw ADO.NET, no stored procedures. No parameterized-SQL risk from hand-written SQL because none exists — all data access is via LINQ-to-Entities (see §7.2 on SQL injection).

### 2.4 Dependency injection

Uses `Microsoft.Extensions.DependencyInjection` directly (no third-party container). Composition root is [App.xaml.cs](MRMS.UI/App.xaml.cs), where **every single service, DbContext, ViewModel, and View is registered as `Transient`** — there is no `Scoped` lifetime in use anywhere (WPF has no natural "request scope", so this is a common simplification, but it has a serious, confirmed side effect — see §5 and §7.3).

Everything is constructor-injected; there is exactly one deliberate service-locator usage (`App.ServiceProvider!.GetRequiredService<...>()` in [RepairTicketDetailPage.xaml.cs:50](MRMS.UI/Views/RepairTicketDetailPage.xaml.cs#L50)) which is a code smell but not itself broken.

---

## 3. Data Model

Verified directly against the live `MRMS` database via `sqlcmd` (not just the EF model, since EF models can drift from reality — see the migration-strategy conflict noted below).

### 3.1 Tables (19, including EF's own history table)

| Table | Rows (live) | Purpose |
|---|---|---|
| `Users` | 1 | Login credentials (SHA-256 password hash, no salt — see §7.2). |
| `Customers` | 2 | Repair-shop customers (name, phone). |
| `Devices` | 2 | Physical phones, FK → `Customers`, unique-ish IMEI. |
| `RepairTickets` | 1 | Core repair job. FK → `Devices`. `Status` stored as `nvarchar` (string-converted enum). |
| `RepairTicketItems` | 1 | Parts consumed by a ticket. FK → `RepairTickets`, `Items`. |
| `DevicePhotos` | 0 | Before/after photos, stores a **file path string**, not the file itself (§5.3). |
| `Warranties` | 1 | 1:1 with `RepairTickets`. |
| `Items` | 4 | Unified catalog for both accessories (sold) and spare parts (consumed in repairs) — one table, `Type` enum distinguishes them. |
| `Suppliers` | 2 | Vendors, has `OpeningBalance` (Excel-migration artifact, see §3.3). |
| `Purchases` / `PurchaseItems` | 4 / 8 | Purchase invoices, header + line items. |
| `Sales` | 4 | POS-style single-line sales (no `SaleItems` table — one row = one item sold; no multi-line "cart" concept, unlike `Purchase`). |
| `Expenses` | 3 | Free-text `Category` (no FK to a categories table — see §3.3). |
| `SupplierPayments` | 4 | Payments against a supplier's running balance. |
| `StockMovements` | 0 | Adjustments (`CustomerReturn`, `SupplierReturn`, `Damaged`) outside normal purchase/sale flow. |
| `SystemSettings` | 2 | Key/value store; only 2 keys used (`PaymentMethods`, `ExpenseCategories`), comma-separated string values. |
| `JournalEntries` / `JournalEntryLines` | 0 / 0 | Double-entry ledger. **Empty despite live transactional data existing that should have posted to it — see §1 and §5.1.** |
| `__EFMigrationsHistory` | 1 | Only one migration (`20260730223517_InitialCreate`) has ever been applied to this database (§3.4). |

### 3.2 Relationships / FK enforcement

FKs are explicitly configured in [AppDbContext.OnModelCreating](MRMS.Infrastructure/Data/AppDbContext.cs) and are real SQL Server foreign keys (not just navigation properties), enforced at the DB level with `DeleteBehavior.Restrict` on almost every FK (preventing accidental cascading deletes of, e.g., a `Customer` with `Devices`), except:
- `DevicePhoto → RepairTicket`: `Cascade` (deleting a ticket deletes its photos — reasonable).
- `Warranty → RepairTicket`: `Cascade` (1:1, reasonable).
- `RepairTicketItem → RepairTicket`, `PurchaseItem → Purchase`, `JournalEntryLine → JournalEntry`: `Cascade` (deleting the header deletes line items — reasonable).

Unique indexes exist on `Users.Username`, `RepairTickets.TicketNumber`, `Items.Code`, `Suppliers.Code`, `SystemSettings.Key`. **`Devices.IMEI` has a non-unique index only** ([AppDbContext.cs:47](MRMS.Infrastructure/Data/AppDbContext.cs#L47) — `HasIndex` without `.IsUnique()`), even though [DeviceService.IsImeiDuplicatedAsync](MRMS.Application/Services/DeviceService.cs#L33-L37) exists specifically to check for duplicates at the application layer. The database itself will silently accept two devices with the same IMEI if that application-level check is ever bypassed or raced — there is no DB-level guarantee, unlike every other "should be unique" field in the schema.

### 3.3 Excel-migration leftovers / schema smells

- **`Supplier.OpeningBalance`**: a classic ledger-migration artifact — a manually-entered starting balance rather than being derived from actual transaction history, used directly in payable calculations ([ReportService.GetPayablesAsync](MRMS.Application/Services/ReportService.cs#L74-L84)). Fine as a one-time migration seam, but there's no `AsOfDate` or audit trail for when/why it was set, and no UI found to actually edit it after supplier creation short of `SupplierFormViewModel`.
- **`Expense.Category` / `Item.Unit`**: free-text strings validated only against a comma-separated list in `SystemSettings` ([SystemSettingService.GetExpenseCategoriesAsync](MRMS.Application/Services/SystemSettingService.cs#L38-L42)), not a real lookup table with an FK. This is a typical "Excel column" pattern (a dropdown backed by a flat list of strings) rather than a normalized relation — nothing stops a bad row from being inserted with an arbitrary category via direct DB access or a future code path that doesn't go through the dropdown.
- **`RepairTicket.ReceiveDate` / `ReceiveTime` and `DeliveryDate` / `DeliveryTime`** are stored as **separate `DateTime` + `TimeSpan` columns** rather than one combined `DateTimeOffset`/`DateTime`. This is unusual for a schema designed from scratch and reads like a direct carry-over from an Excel sheet that had separate "Date" and "Time" columns.
- **`Item` unifies accessories and spare parts in one table** via a `Type` enum — functionally fine, but it means the `Items` catalog conflates two different real-world workflows (retail sale vs. repair consumption) in one CRUD screen ([ItemsPage.xaml](MRMS.UI/Views/ItemsPage.xaml)), which is consistent with a merged Excel "inventory" sheet rather than a purpose-designed schema.
- **All monetary values are stored as `decimal(18,2)`** consistently — this part *is* done correctly and is not a smell.

### 3.4 Migration strategy conflict (real, verified)

`git status` at the start of this audit shows **6 pairs of migration files deleted** and **1 new pair added** (`20260730223517_InitialCreate`) — i.e., the entire migration history was squashed into a single new "InitialCreate" migration, and the live database's `__EFMigrationsHistory` table confirms only that one squashed migration has ever actually been applied (`SELECT * FROM __EFMigrationsHistory` returns exactly 1 row).

However, [SeedService.SeedAsync](MRMS.Infrastructure/Services/SeedService.cs#L21) calls `_context.Database.EnsureCreatedAsync()` — **not** `Database.MigrateAsync()`. `EnsureCreatedAsync()` and EF Core Migrations are mutually incompatible: `EnsureCreatedAsync()` creates the schema directly from the current model snapshot if the DB doesn't exist yet, and does nothing (returns `false`) if it does — it never touches `__EFMigrationsHistory` and cannot apply incremental migrations. The single row currently in `__EFMigrationsHistory` therefore was almost certainly written by someone manually running `dotnet ef database update` from the CLI at some point, not by the running application. **As the code stands today, if the model changes again (a new column, a new table) and a migration is added, the shipped application has no code path that will ever apply it** — `EnsureCreatedAsync()` will just silently no-op against the existing database, and the app will start throwing SQL errors the first time it queries/writes a column that doesn't exist yet in the physical table. This is a production-blocking gap, not a style nitpick.

---

## 4. Module: Repair Lifecycle

### 4.1 State machine as implemented (not as documented)

`TicketStatus` enum ([TicketStatus.cs](MRMS.Core/Enums/TicketStatus.cs)): `Received → UnderInspection → WaitingForParts → InRepair → Repaired → Delivered`, plus a terminal `Cancelled`.

The **only** transition rule enforced in code ([RepairTicketService.UpdateStatusAsync](MRMS.Application/Services/RepairTicketService.cs#L61-L88)) is:

```csharp
if (ticket.Status == TicketStatus.Delivered || ticket.Status == TicketStatus.Cancelled)
    return false;   // no further transitions once Delivered/Cancelled
```

That is the *entire* state machine. **Any other transition is legal** — the code allows jumping straight from `Received` to `Delivered`, or from `Repaired` back to `WaitingForParts`, or bouncing `InRepair → WaitingForParts → InRepair` repeatedly. There is no `switch`/transition table validating "legal next states." The UI ([RepairTicketViewModel](MRMS.UI/ViewModels/RepairTicketViewModel.cs)) exposes status as a free-choice dropdown/index (`SelectedStatusIndex`/`SelectedStatus`), so a technician can set any status at any time.

**This is not theoretical — it already produced real data drift in the live database.** Repair ticket #1 has one `RepairTicketItem` (Item #3, spare-part screen glass, qty 1) but its current status is `Repaired`, and cross-checking `Items.CurrentStock` against every `Purchase`/`Sale`/`RepairTicketItem` row shows Item #3's stock was **never decremented for that part**. The only code path that deducts part stock is `DeductPartsStockAsync`, which only runs on the *first* transition into `InRepair` ([RepairTicketService.cs:78-79](MRMS.Application/Services/RepairTicketService.cs#L78-L79)):

```csharp
if (dto.NewStatus == TicketStatus.InRepair && oldStatus != TicketStatus.InRepair)
    await DeductPartsStockAsync(dto.TicketId);
```

Ticket #1 evidently moved `Received → UnderInspection → Repaired` (or similar) **without ever passing through `InRepair`**, so the part attached to it is simultaneously "consumed by a completed repair" and "counted as available stock" in the `Items` table. This is a live, verified example of the unvalidated state machine causing real inventory inaccuracy, not a hypothetical.

### 4.2 Double-deduction bug (found by code inspection, not yet observed in the small dataset)

The same guard above (`oldStatus != TicketStatus.InRepair`) has the opposite failure mode: it deducts stock for **every existing part on the ticket**, unconditionally, on *every* re-entry into `InRepair` from a different state — not just for parts added since the last deduction. If a ticket cycles `InRepair → WaitingForParts → InRepair` (a legal, unvalidated transition per §4.1), `DeductPartsStockAsync` ([RepairTicketService.cs:219-230](MRMS.Application/Services/RepairTicketService.cs#L219-L230)) re-deducts the full quantity of every part already on the ticket a second time, silently over-decrementing `Item.CurrentStock`. There is no flag on `RepairTicketItem` recording whether it has already been deducted.

### 4.3 Where transitions are triggered from the UI

- `RepairTicketCreateViewModel` / `RepairTicketViewModel.CreateTicketAsync` → always creates with `Status = TicketStatus.Received` ([RepairTicketService.cs:26](MRMS.Application/Services/RepairTicketService.cs#L26)).
- `RepairTicketViewModel.UpdateStatusAsync` ([RepairTicketViewModel.cs:230-256](MRMS.UI/ViewModels/RepairTicketViewModel.cs#L230-L256)) — free-choice status dropdown on the detail page, the main transition entry point.
- `RepairTicketViewModel.UpdateRepairInfoAsync` ([RepairTicketViewModel.cs:271-296](MRMS.UI/ViewModels/RepairTicketViewModel.cs#L271-L296)) — saving repair description/cost **also silently forces the status to `Repaired`** if it wasn't already, as a side effect of what looks like a "save notes" action: `if (SelectedStatus != TicketStatus.Repaired) { SelectedStatus = TicketStatus.Repaired; await _repairTicketService.UpdateStatusAsync(...) }`. This means a technician filling in repair notes mid-job can accidentally flip the ticket to "Repaired" before the work is actually done, since there's no separate confirmation step.

### 4.4 Data-integrity gaps

- **Nothing prevents `Delivered` with an open/zero payment.** `RepairTicket.Cost` is a single free-form decimal set via `UpdateRepairInfoAsync`; there is no "amount paid" field, no partial-payment tracking, and no check before allowing the `Delivered` transition. The only side effect of reaching `Delivered` is a journal post assuming the *full* `Cost` was received in cash on that date ([JournalService.PostRepairDeliveryAsync](MRMS.Application/Services/JournalService.cs#L64-L79)) — and as established in §1/§5.1, that journal post is currently silently lost anyway.
- **`RepairTicket.Cost` can be zero or unset** when marked `Delivered` — no validation requires it to be filled in first.
- **No optimistic concurrency token** on `RepairTicket` — two staff members editing the same ticket concurrently will silently overwrite each other (last write wins), including status.

---

## 5. Module: Accounting / Purchases / Sales / Accessories

### 5.1 What currently works vs. what's broken (specific, verified)

| Area | Status | Evidence |
|---|---|---|
| Purchases (create, add stock) | **Works** | Verified: `Items.CurrentStock` values in the live DB match seed + `PurchaseItems` totals correctly for 3 of 4 items (see §5.3 for the one discrepancy, which is explained by §4.1, not a purchase bug). |
| Sales (create, deduct stock, compute profit) | **Works for create**, **broken for edit** | See below. |
| Sale editing (`SaleService.UpdateAsync`) | **Broken, but currently unreachable from the UI** | [SaleService.cs:78-104](MRMS.Application/Services/SaleService.cs#L78-L104): if `dto.ItemId` differs from the existing sale's item, the stock-quantity `diff` (`dto.Quantity - oldQty`, [line 84](MRMS.Application/Services/SaleService.cs#L84)) is applied to the **new** item ([line 93](MRMS.Application/Services/SaleService.cs#L93) reassigns `existing.ItemId` before the stock adjustment), while the **old** item's stock is never restored. Confirmed no ViewModel/View calls `ISaleService.UpdateAsync` anywhere in `MRMS.UI` (grep found zero call sites) — so this bug is currently dormant/dead code, not yet user-facing, but will corrupt stock the moment an "edit sale" UI is added. Severity: **major** (latent). |
| Journal posting (Sale/Purchase/Expense/RepairDelivery/SupplierPayment/Damaged) | **Broken in production, confirmed via live DB** | See §5.2. Severity: **blocker**. |
| Trial Balance / Journal Entries pages | **Broken as a consequence of the above** | They query `JournalEntries`, which is always empty. |
| Stock movements (`StockMovementService`) | **Works**, logically sound (deducts unless `CustomerReturn`) | [StockMovementService.cs:44-71](MRMS.Application/Services/StockMovementService.cs#L44-L71). |
| Income statement / monthly report / cashbook / payables | **Compute correctly from raw transaction tables** (they do **not** depend on `JournalEntries` — they re-derive revenue/COGS/expenses directly from `Sales`/`RepairTickets`/`Expenses`/`Purchases`), so these reports are unaffected by the journal bug. | [IncomeStatementService.cs](MRMS.Application/Services/IncomeStatementService.cs), [MonthlyReportService.cs](MRMS.Application/Services/MonthlyReportService.cs), [ReportService.cs](MRMS.Application/Services/ReportService.cs) — none of them touch `_uow.JournalEntries`. |
| Test suite | **Does not compile** | `dotnet build MRMS.Tests/MRMS.Tests.csproj` → 4× `CS7036` — `RepairTicketServiceTests.cs` still constructs `new RepairTicketService(mockUow.Object)` with one argument; the constructor has required `IJournalService journalService` as a second parameter since the Phase 7 commit. Confirmed by direct build, not inference. Severity: **blocker** for any CI/regression safety net. |

### 5.2 Journal-posting failure — root cause (blocker, verified via live DB + code)

Every write service that should post a journal entry follows the same pattern, e.g. [SaleService.CreateAsync](MRMS.Application/Services/SaleService.cs#L45-L72):

```csharp
await _unitOfWork.Sales.AddAsync(sale);
await _unitOfWork.CompleteAsync();                 // SaveChanges #1 — persists the Sale

await _journalService.PostSaleAsync(...);           // adds a JournalEntry to _journalService's OWN unit of work
await _unitOfWork.CompleteAsync();                  // SaveChanges #2 — on the WRONG DbContext
```

`SaleService` and `JournalService` are each constructor-injected with `IUnitOfWork`. Because [`AppDbContext` is registered `Transient`](MRMS.Infrastructure/DependencyInjection.cs#L15-L17) and [`IUnitOfWork` is registered `Transient`](MRMS.Infrastructure/DependencyInjection.cs#L19), and because both `SaleService` and `IJournalService` are *independently* resolved from the container when `SaleService`'s constructor is built, **`SaleService._unitOfWork` and `JournalService._uow` wrap two different `AppDbContext` instances**. `JournalService.PostSaleAsync` calls `_uow.JournalEntries.AddAsync(...)` ([JournalService.cs:19-31](MRMS.Application/Services/JournalService.cs#L19-L31)), which stages the new `JournalEntry` in *its own* context's change tracker — a context that `SaleService` never calls `SaveChanges` on. `SaleService`'s second `_unitOfWork.CompleteAsync()` call saves *its* context, which has no pending `JournalEntry` changes, so it's a no-op for that entity. The `JournalEntry` is silently discarded when `JournalService`'s (and its `AppDbContext`'s) transient lifetime ends, with **no exception, no log entry, nothing** — it just vanishes.

**Empirical confirmation:** the live database has 4 `Sales`, 4 `Purchases`, 4 `SupplierPayments`, and 3 `Expenses` rows, several with `CreatedAt` timestamps (2026-08-03/04) that postdate the `JournalService` commit (`e50290e`, 2026-07-30) by days — i.e., these were created by the running app *after* the journal-posting code existed and should have fired — yet `JournalEntries` has 0 rows and `JournalEntryLines` has 0 rows.

This exact pattern (`IJournalService` injected alongside `IUnitOfWork`, then calling `_unitOfWork.CompleteAsync()` after the journal call) repeats identically in:
- [RepairTicketService.UpdateStatusAsync](MRMS.Application/Services/RepairTicketService.cs#L82-L86) (delivery revenue journal)
- [PurchaseService.CreateAsync](MRMS.Application/Services/PurchaseService.cs#L61-L65)
- [ExpenseService.CreateAsync](MRMS.Application/Services/ExpenseService.cs#L54-L58)
- [SupplierPaymentService.CreateAsync](MRMS.Application/Services/SupplierPaymentService.cs#L47-L55)
- [StockMovementService.CreateAsync](MRMS.Application/Services/StockMovementService.cs#L73-L78) (damaged-stock write-off journal)

**All six posting paths are affected.** This is the single highest-priority fix in the codebase; everything built on top of the ledger (Trial Balance, Journal Entries list) is currently vestigial UI showing permanently empty data.

### 5.3 Profit calculation — formula and correctness

`Sale.Profit` ([Sale.cs:12](MRMS.Core/Entities/Sale.cs#L12)): `Profit => TotalPrice - (Quantity * CostPrice)`, where `Sale.CostPrice` is a **snapshot value captured at sale time**, auto-filled from `Item.CostPrice` in the UI when an item is selected ([SaleFormViewModel.cs:63-70](MRMS.UI/ViewModels/SaleFormViewModel.cs#L63-L70)) but passed through as an ordinary editable `decimal` on `CreateSaleDto` — **the service layer never re-validates it against the item's actual cost** ([SaleService.CreateAsync](MRMS.Application/Services/SaleService.cs#L45-L72) takes `dto.CostPrice` as-is). A user can change the pre-filled cost price before saving, which will silently skew the profit figure reported everywhere (income statement, monthly report, dashboard) — there's no server-side guard. This may be intentional (e.g. to allow post-hoc cost corrections), but nothing in the code or UI communicates that intent; flagged as an **open question** (§9).

Gross profit at the income-statement level ([IncomeStatementService.GetIncomeStatementAsync](MRMS.Application/Services/IncomeStatementService.cs#L19-L60)) is:

```
salesRevenue   = Σ Sale.TotalPrice
repairRevenue  = Σ RepairTicket.Cost (Delivered tickets only)
cogs           = Σ Sale.Quantity × Sale.CostPrice
repairPartsCost= Σ RepairTicketItem.TotalCost (parts on Delivered tickets)
grossProfit    = totalRevenue − cogs − repairPartsCost
netProfit      = grossProfit − operatingExpenses − damageLosses
```

This formula does **not** account for:
- **Returns** — `StockMovement.CustomerReturn`/`SupplierReturn` exist as a concept but are entirely excluded from the income statement; a customer return reduces stock-in-hand bookkeeping only, never reverses the original sale's revenue/profit.
- **Discounts** — there is no discount field anywhere in `Sale`, `Purchase`, or `RepairTicket`; a discounted sale can only be represented by manually lowering `UnitPrice`, which is fine for revenue but means there's no separate "discount" line for reporting/analysis.
- **Partial payments** — as noted in §4.4, `RepairTicket.Cost` and `Purchase.PaidAmount` are both treated as fully-realized cash amounts on their respective dates with no partial/outstanding tracking (`Purchase.PaidAmount` in particular is misleadingly named — it is used both as "amount owed" input on create and directly as the purchase's cash-outflow figure in every cash-flow calculation, with no distinction between "invoice total" and "amount actually paid," so a purchase made partly on credit will misstate both payables and cash flow).

### 5.4 Inventory concurrency

**No optimistic concurrency control exists anywhere on `Item.CurrentStock`.** There is no `[Timestamp]`/`RowVersion` column, and stock checks follow a classic **check-then-act race**: e.g. [SaleService.CreateAsync](MRMS.Application/Services/SaleService.cs#L58-L61):

```csharp
if (item.CurrentStock < dto.Quantity)
    throw new InvalidOperationException(...);
item.CurrentStock -= dto.Quantity;
```

Two simultaneous sales of the same item, each reading `CurrentStock` before either writes, can both pass the check and both decrement — allowing oversell. Given `AppDbContext`/`IUnitOfWork` are `Transient` (a new one per service call) rather than sharing a single ambient transaction/scope, there is nothing serializing these reads-then-writes at the application level either. This is realistic risk for a shop with more than one till/counter, less so for a single-technician desktop app, but it is a genuine gap for any future multi-seat deployment. Same pattern appears in `PurchaseService`, `StockMovementService`, and `RepairTicketService`'s part-deduction paths.

### 5.5 Bug list summary (accounting module)

| # | Description | Location | Severity |
|---|---|---|---|
| 1 | Journal entries never persist (DI transient-lifetime mismatch) | `SaleService.cs`, `PurchaseService.cs`, `ExpenseService.cs`, `SupplierPaymentService.cs`, `StockMovementService.cs`, `RepairTicketService.cs` (all call sites of `IJournalService`) + [Infrastructure/DependencyInjection.cs](MRMS.Infrastructure/DependencyInjection.cs#L15-L19) | **Blocker** |
| 2 | `SaleService.UpdateAsync` corrupts stock when the item is changed on edit | [SaleService.cs:78-104](MRMS.Application/Services/SaleService.cs#L78-L104) | Major (currently dead code — no UI calls it) |
| 3 | Repair-ticket status has no transition validation; already caused stock/part drift in live data | [RepairTicketService.cs:61-88](MRMS.Application/Services/RepairTicketService.cs#L61-L88) | Major |
| 4 | Part-stock double-deduction on re-entering `InRepair` | [RepairTicketService.cs:219-230](MRMS.Application/Services/RepairTicketService.cs#L219-L230) | Major |
| 5 | No optimistic concurrency on `Item.CurrentStock`; check-then-act race on every stock-affecting write | `SaleService`, `PurchaseService`, `StockMovementService`, `RepairTicketService` | Minor–Major (depends on deployment concurrency) |
| 6 | `Purchase.PaidAmount` conflates "invoice total owed" and "cash actually paid"; no partial-payment/credit tracking anywhere | `Purchase.cs`, `PurchaseService.cs`, all cash-flow/payables calculations | Major |
| 7 | `Sale.CostPrice` is user-editable at save time with no server-side validation against `Item.CostPrice`, silently affecting all profit reporting | [SaleFormViewModel.cs:63-70](MRMS.UI/ViewModels/SaleFormViewModel.cs#L63-L70), [SaleService.cs:45-72](MRMS.Application/Services/SaleService.cs#L45-L72) | Minor (possibly intentional — see §9) |
| 8 | Income statement ignores returns and has no discount/partial-payment model | [IncomeStatementService.cs](MRMS.Application/Services/IncomeStatementService.cs) | Major |
| 9 | Test suite does not compile (stale constructor signature) | [MRMS.Tests/Services/RepairTicketServiceTests.cs](MRMS.Tests/Services/RepairTicketServiceTests.cs) | Blocker (for CI/regression safety) |

---

## 6. Cross-Module Integration Points

| Integration | Implemented? | Detail |
|---|---|---|
| Repair delivery → journal revenue entry | **Coded, but broken** | §5.2 — the call exists ([RepairTicketService.cs:82-86](MRMS.Application/Services/RepairTicketService.cs#L82-L86)) but never persists. |
| Repair part usage → accessories/spare-parts stock | **Implemented, but gated on the unvalidated status state machine** | Only decrements when a ticket first enters `InRepair` (§4.1/§4.2) — parts added to a ticket that skips `InRepair` entirely are never deducted, as demonstrated by ticket #1 in the live data. |
| Sale → stock deduction | **Implemented and correct on create** | [SaleService.cs:63](MRMS.Application/Services/SaleService.cs#L63). Broken on edit (§5.5 #2). |
| Purchase → stock increment | **Implemented and correct** | [PurchaseService.cs:47-51](MRMS.Application/Services/PurchaseService.cs#L47-L51). |
| Shared `Item` catalog between POS sales and repair parts | **Yes, by design** — one `Items` table, `ItemType` enum distinguishes `Accessory`/`SparePart`, but both draw from the same `CurrentStock` | This is a real, working integration point — a spare part consumed in a repair and an accessory sold at the counter both hit the same inventory ledger table (`StockMovements`/`Items.CurrentStock`), so stock levels are unified. No separate "repair parts" vs "retail" stock pools. |
| Shared `Customer` table | **Yes** — `Customer` owns `Devices`, and `Sale.CustomerName` is a **free-text string**, not a FK to `Customer` | Sales are *not* linked to the `Customers` table at all ([Sale.cs:10](MRMS.Core/Entities/Sale.cs#L10) — `string CustomerName`), so a customer's full purchase history (accessories) cannot be queried from their customer profile the way their repair history can (`CustomerProfilePage`/`CustomerProfileViewModel` only ever shows devices/repairs, never accessory sales). This is an inconsistency: the repair side treats `Customer` as a first-class entity; the sales side does not. |
| Supplier payables ↔ purchases | **Implemented and consistent** | `ReportService.GetPayablesAsync` / `DashboardService.CalculatePayablesTotalAsync` both derive outstanding balance the same way (`OpeningBalance + Σ PurchaseItems.TotalCost − Σ SupplierPayments.Amount`), so no divergence found there. |
| Dashboard KPIs pulling from both modules | **Implemented** | [DashboardService.cs](MRMS.Application/Services/DashboardService.cs) combines `IRepairTicketService` stats with sales/expense/purchase aggregates — this is the one place that legitimately spans both modules cleanly, though it constructs a `new IncomeStatementService(_uow)` manually ([DashboardService.cs:78](MRMS.Application/Services/DashboardService.cs#L78)) rather than injecting `IIncomeStatementService`, which is inconsistent with DI usage everywhere else (works correctly here only because it deliberately reuses the same `_uow` instance — an implicit acknowledgment, in one place, of the exact DbContext-per-instance problem that breaks journal posting everywhere else). |

**Where the modules *should* connect but don't:** repair-ticket delivery has no link to a `Sale` record — delivering a repaired device with `Cost` set is treated purely as revenue via the (broken) journal post, never as a `Sale`-shaped transaction, so repair revenue and accessory-sale revenue live in structurally different tables reconciled only by ad-hoc `Union`-style LINQ in report services (`IncomeStatementService`, `MonthlyReportService`, `ReportService`, `DashboardService` — the same `salesRevenue + repairRevenue` pattern is manually re-implemented in at least four separate services rather than centralized once).

---

## 7. Code Quality & Risk Findings

### 7.1 Error handling gaps

- **Every ViewModel command catches `Exception` broadly and surfaces `ex.Message` directly to the Arabic-language UI** (24+ occurrences, e.g. [ExpenseFormViewModel.cs:74-77](MRMS.UI/ViewModels/ExpenseFormViewModel.cs#L74-L77), [DeviceRegistrationViewModel.cs](MRMS.UI/ViewModels/DeviceRegistrationViewModel.cs) ×5, [RepairTicketViewModel.cs](MRMS.UI/ViewModels/RepairTicketViewModel.cs) ×4). This means raw .NET/EF exception text (potentially including SQL error detail) can reach the end user, and **almost none of these catch blocks log the exception** — only [LoginViewModel.cs:53](MRMS.UI/ViewModels/LoginViewModel.cs#L53) calls `Log.Error`. Every other swallowed exception across ~20 ViewModels is invisible to Serilog, meaning production failures leave no trace in `logs/mrms-*.log` unless they happen to be unhandled and hit `App.xaml.cs`'s global `DispatcherUnhandledException` handler ([App.xaml.cs:20-24](MRMS.UI/App.xaml.cs#L20-L24), which itself also just logs and marks `Handled = true` — swallowing the crash and continuing, which can leave the app in an undefined state after a serious failure).
- **Seed failure is deliberately swallowed** ([App.xaml.cs:122-126](MRMS.UI/App.xaml.cs#L122-L126)) with a comment `"Database seeding failed (non-fatal)"` — reasonable for seeding specifically, but combined with the `EnsureCreatedAsync()` vs. migrations conflict (§3.4), a schema mismatch here would silently leave the app running against a stale/incompatible schema with only a `Log.Warning` and no user-facing signal.
- **No input validation layer** (no FluentValidation/DataAnnotations) — validation is ad hoc `if (x <= 0) throw new InvalidOperationException("...")` scattered per-service (present in `SaleService`, `ExpenseService`, `StockMovementService`; **absent** in `CustomerService`, `DeviceService`, `SupplierService`, `ItemService` — e.g. `ItemService.CreateAsync` has zero validation on `CostPrice`/`SellingPrice`/`Code` uniqueness before hitting the DB's unique-index constraint, so a duplicate `Item.Code` throws a raw `DbUpdateException` up to the ViewModel's generic catch block rather than a friendly message).

### 7.2 Security issues

- **Hardcoded SQL Server connection string in two source files**, including a real machine name, committed to source control: [App.xaml.cs:34](MRMS.UI/App.xaml.cs#L34) and [AppDbContextFactory.cs:12](MRMS.Infrastructure/Data/AppDbContextFactory.cs#L12) (`Server=ABDO-DESKTOP\SQLEXPRESS;Database=MRMS;Trusted_Connection=True;...`). No `appsettings.json`, no environment-variable override, no `.env` (the `.gitignore` explicitly excludes `.env`/`secrets.json`/`appsettings.Development.json` — suggesting config externalization was planned but never implemented). This will break entirely on any machine other than the original developer's, and hardcodes topology into source.
- **Passwords hashed with unsalted SHA-256** ([AuthService.cs:16-17](MRMS.Application/Services/AuthService.cs#L16-L17), [SeedService.cs:24-25](MRMS.Infrastructure/Services/SeedService.cs#L24-L25)) — no salt, no iteration/work-factor (not bcrypt/PBKDF2/Argon2). Vulnerable to rainbow-table attack if the `Users` table is ever exfiltrated, and the seeded default account (`tech` / `admin123`) is a well-known default credential shipped directly in source.
- **No SQL injection risk found** — all queries are LINQ-to-Entities via EF Core; no string-concatenated SQL exists anywhere in the codebase (grep confirmed no raw ADO.NET/`ExecuteSqlRaw` usage).
- **No secrets manager, no encryption at rest** for the connection string or any other config value.
- **Device photos referenced by raw absolute file path** ([RepairTicketDetailPage.xaml.cs:76-88](MRMS.UI/Views/RepairTicketDetailPage.xaml.cs#L76-L88)) — `PhotoService.AddAsync` stores whatever path `Microsoft.Win32.OpenFileDialog` returns (e.g. `C:\Users\<tech>\Desktop\photo.jpg`) directly into the DB, rather than copying the file into an app-managed storage folder. This breaks the moment the source file is moved/deleted, and would break entirely in any multi-machine deployment (the DB is centrally shared via SQL Server, but photo files are not — a photo added on one workstation is unreachable from another). This is a real functional defect for anything beyond a single-PC deployment, not just a security note.

### 7.3 Concurrency / transaction issues

- **No explicit database transactions anywhere in the codebase** (confirmed: no `BeginTransaction`/`TransactionScope`/`IDbContextTransaction` usage found via grep across the whole solution). Every multi-step write that should be atomic (e.g., "add a `Sale` row AND decrement `Item.CurrentStock` AND post a journal entry") is split across **two separate `SaveChangesAsync()` calls on two different `DbContext` instances** (§5.2), so there is no atomicity guarantee even within a single business operation, let alone the additional cross-context problem. A crash or exception between the two `CompleteAsync()` calls in, e.g., `SaleService.CreateAsync` leaves the sale + stock decrement committed but the (already-broken) journal post skipped — not a new failure mode, just compounding the existing one.
- **`AppDbContext` and `IUnitOfWork` registered `Transient`** ([Infrastructure/DependencyInjection.cs:15-19](MRMS.Infrastructure/DependencyInjection.cs#L15-L19)) is the root architectural cause of §5.2 and is almost certainly unintentional — a `Scoped`-per-operation (or a shared `Scoped` per ViewModel-resolution `IServiceScope`) pattern is the standard fix, but nothing in this codebase currently creates such a scope around a logical business operation.
- No row-versioning/optimistic concurrency tokens anywhere in the model (§5.4).

### 7.4 UI logic duplicated across multiple forms

- **The Arabic display-text mapping for `TicketStatus` is independently duplicated in four places**, and two of those four disagree with each other:
  - [TicketStatusConverter.cs:15-21](MRMS.UI/Converters/TicketStatusConverter.cs#L15-L21) and [RepairTicketListViewModel.cs:31](MRMS.UI/ViewModels/RepairTicketListViewModel.cs#L31) both say **"قيد الفحص"** for `UnderInspection`.
  - [RepairTicketViewModel.cs:252](MRMS.UI/ViewModels/RepairTicketViewModel.cs#L252) and [PrintPreviewWindow.xaml.cs:120](MRMS.UI/Views/PrintPreviewWindow.xaml.cs#L120) both say **"تحت الفحص"** for the same status.

  Depending on which screen a user is looking at (list view/converter vs. the success-toast after changing status vs. the printed ticket), the same `UnderInspection` status is labeled with two different Arabic phrases. This is a direct, user-visible symptom of the lack of a single source of truth for status display text — it should be one method/resource, referenced everywhere, not four hand-copied `switch` statements.
- **Revenue/profit aggregation formula (`salesRevenue + repairRevenue`, `cogs`, `grossProfit`) is independently re-implemented** in `IncomeStatementService`, `MonthlyReportService`, `ReportService` (cashbook), and `DashboardService` rather than being computed once and reused — confirmed by reading all four; the LINQ shapes are near-identical but not shared via a common helper/query object.
- **Payables-outstanding calculation** (`OpeningBalance + Σ purchases − Σ payments`) is duplicated verbatim in `ReportService.GetPayablesAsync`, `DashboardService.CalculatePayablesTotalAsync`, and `DashboardService.CountOverduePayablesAsync` — three separate LINQ blocks doing the same math.

---

## 8. Production Readiness Gap List

Ordered by risk (highest first), not by implementation effort:

1. **Journal/ledger subsystem is completely non-functional in production** (§5.2) — must be fixed before the accounting module can be trusted at all; Trial Balance and Journal Entries screens are currently decorative.
2. **Migration strategy is broken** (`EnsureCreatedAsync()` vs. an EF Migrations history that nothing in the shipped app actually applies) — the next schema change will not deploy correctly to any existing installation (§3.4).
3. **No config/secrets management** — hardcoded connection string with a real hostname in two files; will not run on any machine but the original developer's without a source change and rebuild (§7.2).
4. **No transactional integrity** around any multi-table write — partial failures leave the DB in inconsistent states with no rollback (§7.3).
5. **Repair-ticket status has no enforced state machine** — already causing verified inventory drift in the live dataset (§4.1); will get worse as more tickets are processed.
6. **Test suite does not compile** — there is currently zero automated regression protection; any future change is unverifiable except by manual clicking (§5.1, §5.5 #9).
7. **No stock concurrency control** — fine for single-user use today, a blocker the moment a second workstation/till is introduced (§5.4).
8. **Device photos stored as raw local file paths**, not copied into managed storage — breaks under any multi-machine deployment and is fragile even single-machine if files are reorganized (§7.2).
9. **Passwords are unsalted SHA-256**, and a default admin account ships in source (`tech`/`admin123`) — should move to a modern password hash (e.g. ASP.NET Core Identity's hasher, or BCrypt/Argon2) before any real credential is stored this way (§7.2).
10. **No structured input validation layer** — validation is inconsistent across services, some entirely missing (§7.1), allowing raw DB constraint violations to surface as unhandled exceptions to end users.
11. **Logging is inconsistent** — most caught exceptions are never logged (§7.1), meaning production incidents will often leave zero trace in `logs/mrms-*.log`.
12. **No backup strategy documented anywhere** in the repo (no scripts, no scheduled task references, no README mention) — for a shop's financial records, this is a material business risk independent of code quality.
13. **No deployment process documented** — no installer project, no publish profile, no CI/CD pipeline (no `.github/workflows` or equivalent found), no versioning strategy visible in the repo.
14. **UI-layer business-logic duplication** (§7.4) — status text and financial formulas re-implemented per-screen; low risk today, but a maintenance/consistency hazard as the app grows (already manifesting as the status-text mismatch).

---

## 9. Open Questions

These could not be determined from the code alone and need the developer/business owner to clarify:

1. **Is `Sale.CostPrice` meant to be user-editable at sale time**, or should it always be locked to `Item.CostPrice` at the moment of sale (with cost corrections handled some other way)? Currently it's auto-filled but silently overridable, which affects every profit report (§5.3).
2. **What is the intended full repair-ticket state machine?** The enum defines 7 states but the code enforces no transition rules beyond "nothing changes after Delivered/Cancelled." Is skipping `InRepair` when no parts are needed intentional, and if so, should part-stock deduction be decoupled from that specific status transition instead (e.g. deduct at the moment a part is *added*, regardless of ticket status)?
3. **How should partial repair payments / partial purchase payments be represented?** `RepairTicket.Cost` and `Purchase.PaidAmount` are both currently treated as single, fully-realized amounts with no partial/outstanding-balance concept — is this by design (cash-only shop, no credit to customers) or a missing feature?
4. **Should accessory sales be linked to `Customer`?** Currently `Sale.CustomerName` is free text, unlike the repair side where `Customer` is a full relational entity. Is walk-in/anonymous accessory sale intentional, or should sales optionally link to an existing customer record?
5. **What is the intended behavior for `StockMovement.SupplierReturn` and `CustomerReturn` in financial reporting?** They currently adjust `Item.CurrentStock` but are excluded from the income statement / profit calculations entirely — is that intentional (returns are stock-only events) or should a customer return also reverse the original sale's revenue?
6. **Is multi-workstation/multi-till usage in scope?** This materially changes the priority of the concurrency (§5.4) and photo-storage (§7.2) findings — both are low-risk for a single PC and high-risk for a shared/networked deployment.
7. **What is the target deployment model** (single PC, shared network SQL Server with multiple clients, cloud-hosted)? This affects the priority ordering of §8 significantly (e.g., config externalization matters far more for multi-machine deployment).
8. **Was the migration squash** (6 old migration files deleted, replaced with one new `InitialCreate`) **a deliberate reset**, and if so, is the plan to keep using `EnsureCreatedAsync()` going forward, or was `Database.MigrateAsync()` intended to replace it and just not yet wired in (§3.4)?

---

*This audit is a point-in-time technical baseline (as of 2026-08-14) intended to inform future bug-fix, refactor, and feature work. It intentionally makes no code changes.*
