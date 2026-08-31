# Implementation Plan — Full Accounting Desktop App

**Target:** Evolve MRMS from a repair-ticket manager into a production-ready accounting system for a mobile accessories & repair shop.

**Current:** 6 entities (User, Customer, Device, RepairTicket, DevicePhoto, Warranty), basic dashboard, no accounting.

**Target:** 16-sheet Excel-equivalent system with full inventory, purchasing, sales, expenses, payables, monthly reports with charts.

---

## Phase 1: Foundation — Core Domain Entities

**Goal:** Establish the inventory catalog, supplier management, and system settings.

### 1.1 New Entities (MRMS.Core)

| Entity | Key Properties |
|---|---|
| `Item` | `Id`, `Code`, `Name`, `Type` (Accessory/SparePart), `Unit`, `CostPrice`, `SellingPrice`, `ReorderLevel`, `CurrentStock`, `CreatedAt` |
| `Supplier` | `Id`, `Code`, `Name`, `Phone`, `OpeningBalance`, `Notes`, `CreatedAt` |
| `SystemSetting` | `Id`, `Key`, `Value` (payment methods, expense categories, etc.) |

### 1.2 New Enums
- `ItemType` : `Accessory`, `SparePart`
- `PaymentMethod` : `Cash`, `Credit`
- `MovementType` : `CustomerReturn`, `SupplierReturn`, `Damaged`

### 1.3 Schema Updates
- Add `ItemId` FK and `CostPrice` to **RepairTicket** (linking parts consumed per ticket via a new join table `RepairTicketItems`)
- Add `ItemId` FK to **DevicePhoto**? No — unrelated.

### 1.4 Database Migration
- Create `Items`, `Suppliers`, `SystemSettings`, `RepairTicketItems` tables
- Seed default expense categories, payment methods

### 1.5 Repository & UnitOfWork Updates
- Add `Items`, `Suppliers`, `SystemSettings`, `RepairTicketItems` repositories
- Update `IUnitOfWork` interface

### 1.6 Services (MRMS.Application)
- `IItemService` — CRUD, stock queries, low-stock alerts
- `ISupplierService` — CRUD, balance calculation
- `ISystemSettingService` — key-value store for dropdowns

### 1.7 UI Pages (MRMS.UI)
- `ItemsPage` — DataGrid + add/edit form for the item catalog
- `SuppliersPage` — DataGrid + add/edit form for supplier list
- `SettingsPage` — manage payment methods, expense categories

---

## Phase 2: Core Transactions — Purchases, Sales, Expenses

**Goal:** Enable daily transaction entry for purchasing stock, selling items (cash), and recording expenses.

### 2.1 New Entities

| Entity | Key Properties |
|---|---|
| `Purchase` | `Id`, `Date`, `SupplierId` (FK), `PaymentMethod`, `PaidAmount`, `Notes`, `CreatedAt` |
| `PurchaseItem` | `Id`, `PurchaseId` (FK), `ItemId` (FK), `Quantity`, `UnitCost`, `TotalCost` (formula) |
| `Sale` | `Id`, `Date`, `ItemId` (FK), `Quantity`, `UnitPrice`, `TotalPrice` (formula), `CostPrice` (from Item), `Profit` (formula), `CustomerName` (optional), `Notes`, `CreatedAt` |
| `Expense` | `Id`, `Date`, `Category`, `Description`, `Amount`, `PaymentMethod`, `Notes`, `CreatedAt` |

### 2.2 Business Logic
- **Purchase**: Adds items to `Item.CurrentStock`
- **Sale**: Deducts from `Item.CurrentStock`, calculates profit
- **Expense**: Pure cash outflow, no stock impact
- Auto-generate purchase order numbers and sale invoice numbers

### 2.3 Services
- `IPurchaseService` — create purchase with items, stock update, supplier balance update
- `ISaleService` — create sale, deduct stock, calculate profit
- `IExpenseService` — CRUD expenses

### 2.4 UI Pages
- `PurchasesPage` — purchase list + create form with multi-line item entry (like a real invoice)
- `SalesPage` — point-of-sale style: select item, enter qty, show total, record
- `ExpensesPage` — expense entry form + list with date filtering

### 2.5 Enhanced Repair — Unlimited Parts
- Create `RepairTicketItem` entity: `Id`, `RepairTicketId` (FK), `ItemId` (FK), `Quantity`, `UnitCost`
- Update existing `RepairTicketCreatePage` / `RepairTicketDetailPage` to allow adding multiple parts via a DataGrid
- Deduct selected parts from `Item.CurrentStock` when ticket status moves to `InRepair`
- Calculate total parts cost per ticket via `SUMIF` equivalent (LINQ sum)

---

## Phase 3: Financial Tracking — Supplier Payments, Returns/Damages

**Goal:** Track supplier credit payments and inventory adjustments (returns, damage write-offs).

### 3.1 New Entities

| Entity | Key Properties |
|---|---|
| `SupplierPayment` | `Id`, `Date`, `SupplierId` (FK), `Amount`, `Reference`, `Notes`, `CreatedAt` |
| `StockMovement` | `Id`, `Date`, `MovementType`, `ItemId` (FK), `Quantity`, `Value`, `ReferenceId` (nullable), `Notes`, `CreatedAt` |

### 3.2 Business Logic
- **Supplier Payment**: Reduces supplier's outstanding balance
- **Stock Movements** (Returns/Damages):
  - `CustomerReturn` → increases `Item.CurrentStock` (+qty)
  - `SupplierReturn` → decreases `Item.CurrentStock` (-qty), reduces supplier balance
  - `Damaged` → decreases `Item.CurrentStock` (-qty), records loss

### 3.3 Services
- `ISupplierPaymentService` — record payment, update supplier balance
- `IStockMovementService` — record returns/damages, adjust stock

### 3.4 UI Pages
- `SupplierPaymentsPage` — payment form + history per supplier
- `StockMovementsPage` — returns/damages entry form + list

---

## Phase 4: Core Reports — Stock Balance, Cashbook, Payables

**Goal:** Real-time financial snapshots — no manual data entry, all formula-driven.

### 4.1 Report Pages (Read-Only Views)

| Report | Logic |
|---|---|
| **StockBalancePage** | For each Item: `CurrentStock`, `StockValue = CurrentStock * CostPrice`, flag if below `ReorderLevel` |
| **CashbookPage** | Opening Balance + (Sales + Repair Income) - (Cash Purchases + Expenses + Supplier Payments) = Closing Balance |
| **PayablesPage** | Per Supplier: Opening + Credit Purchases - Payments = Outstanding Balance |

### 4.2 No new entities needed
All data derived from existing transaction tables.

### 4.3 Services
- `IReportService` (or individual query methods on existing services)
  - `GetStockBalanceReport()`
  - `GetCashbookAsync(DateTime from, DateTime to)`
  - `GetPayablesReport()`

### 4.4 UI Pages
- `StockBalancePage` — grid with stock alerts (red for low stock)
- `CashbookPage` — register-style view with running balance, date range filter
- `PayablesPage` — supplier balances with drill-down to payment history

---

## Phase 5: Income Statement & Enhanced Dashboard

**Goal:** Full P&L statement and a dashboard that shows period-based performance.

### 5.1 Income Statement (Read-Only)

```
Total Sales Revenue (from Sales)
+ Total Repair Revenue (from RepairTickets)
= **Total Revenue**
- Cost of Goods Sold (sum of Sale.CostPrice)
- Cost of Repair Parts (sum of RepairTicketItem)
= **Gross Profit**
- Operating Expenses (sum of Expenses)
- Damage Losses (sum of StockMovements where Damaged)
= **Net Profit**
```

### 5.2 Enhanced Dashboard
- Add **period selector** (current month, previous month, custom range)
- Cards: Net Profit, Revenue, Expenses, Cash Balance, Stock Value, Payables Total
- Compare current period vs previous period with % change (▲▼)
- Quick links to low-stock items and overdue payables

### 5.3 Services
- `IIncomeStatementService` — compute P&L for a date range
- `IDashboardService` — aggregated KPIs with period comparison

### 5.4 UI Pages
- `IncomeStatementPage` — formatted P&L report, printable
- Updated `DashboardPage` — new cards + period filter

---

## Phase 6: Monthly Report with Charts

**Goal:** Visual monthly performance report answering "How much did I earn/spend this month vs last month?"

### 6.1 Monthly Report Page

**Controls:**
- Year dropdown (default: current)
- Month dropdown (default: current)
- Comparison: selected month vs previous month

**Sections:**
1. **KPI Cards** — Revenue, Expenses, Net Profit, Cash Balance — with ▲▼ % change
2. **12-Month Table** — all months of selected year with: Sales, Repair Revenue, Total Revenue, COGS, Parts Cost, Gross Profit, Expenses, Net Profit, Net Cash Flow, Cumulative Cash Balance
3. **Deep Analysis:**
   - Revenue breakdown (Sales vs Repairs) — stacked bar / pie
   - Monthly cash balance trend
   - Top 10 best-selling items by revenue

### 6.2 Charts (4 total)

| Chart | Type | Data |
|---|---|---|
| Net Profit Monthly | Column chart | 12 months × Net Profit |
| Income vs Expenses | Dual column chart | 12 months × (Income, Expenses) |
| Sales vs Repair Revenue | Stacked column | 12 months × (Sales, Repair) |
| Top 10 Items | Horizontal bar | Top items by total sales value |

### 6.3 Technology
- Use a charting library (e.g., **LiveCharts2** for WPF, or **OxyPlot**, or **System.Windows.Controls.DataVisualization**)
- Recommend: **LiveCharts2.WPF** — modern, animated, supports RTL, lightweight

### 6.4 Service
- `IMonthlyReportService` — aggregates all monthly data, returns report DTO

### 6.5 UI Page
- `MonthlyReportPage` — chart-heavy view with year/month selectors

---

## Phase 7: Advanced Accounting (Optional / Deferred)

**Goal:** Double-entry accounting — Journal Entries & Trial Balance.

### 7.1 New Entity
- `JournalEntry` : `Id`, `Date`, `Description`, `ReferenceType`, `ReferenceId`, `CreatedAt`
- `JournalEntryLine` : `Id`, `JournalEntryId` (FK), `AccountName`, `DebitAmount`, `CreditAmount`

### 7.2 Auto-Posting
- Every transaction (Sale, Purchase, Expense, Payment, etc.) auto-generates journal entries
- Debit/Credit mapping per transaction type

### 7.3 Trial Balance
- Sum all debits and credits per account
- Verify: Total Debits == Total Credits

### 7.4 UI Pages
- `JournalEntriesPage` — view auto-generated entries
- `TrialBalancePage` — grid + balance verification

---

## Phase 8: Production Polish & Deployment

**Goal:** Make the app production-ready.

### 8.1 Data Validation & Protection
- Protect formula/summary cells from editing (read-only views)
- Input validation on all forms (required fields, positive quantities, valid dates)
- Confirmation dialogs on destructive actions (delete, cancel ticket)

### 8.2 UX Improvements
- RTL layout verified across all new pages
- Keyboard shortcuts (F2=search, F5=refresh, Ctrl+N=new)
- Loading indicators on all async operations
- Error handling with user-friendly messages

### 8.3 Performance
- Pagination on large lists (Sales, Purchases, Expenses)
- Lazy loading for dashboard reports
- Indexed queries for monthly aggregations

### 8.4 Data Integrity
- Ensure all stock deductions happen transactionally
- Prevent negative stock (configurable)
- Audit log for sensitive changes (optional)

### 8.5 Testing
- Unit tests for all new services
- Integration tests for stock balance calculations
- UI smoke tests for new pages

---

## Summary: Phases & Effort

| Phase | Focus | New Entities | New Pages | Effort |
|---|---|---|---|---|
| **1** | Foundation (Items, Suppliers, Settings) | 3 | 3 | High |
| **2** | Core Transactions (Purchases, Sales, Expenses, Repair Parts) | 4 | 4 | Very High |
| **3** | Financial Tracking (Supplier Payments, Returns) | 2 | 2 | Medium |
| **4** | Core Reports (Stock, Cashbook, Payables) | 0 | 3 | Medium |
| **5** | Income Statement & Enhanced Dashboard | 0 | 2 | Medium |
| **6** | Monthly Report & Charts | 0 | 1 | High |
| **7** | Advanced Accounting (Journal, TB) | 2 | 2 | High (deferred) |
| **8** | Polish, Testing, Deployment | 0 | 0 | Medium |

**Total new entities:** ~11 (Phases 1-3, 7)  
**Total new pages:** ~20  
**Core library:** LiveCharts2.WPF for charting

**Recommended sequencing:** Phases 1→2→3→4→5→6→8 (Phase 7 is optional, defer until after production launch).
