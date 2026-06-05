# MRMS — Mobile Repair Management System

## [TECH_STACK]
- **Runtime:** .NET 10.0.8 LTS (Windows)
- **UI:** WPF (.NET 10)
- **ORM:** EF Core 10.0.8 + SQL Server Provider
- **MVVM:** CommunityToolkit.Mvvm 8.4.2 (source generators)
- **DI:** Microsoft.Extensions.DependencyInjection 10.0.8
- **Logging:** Serilog 4.3.1 + Serilog.Sinks.File 7.0.0
- **Database:** SQL Server 2025 Express (`ABDO-DESKTOP\SQLEXPRESS`)
- **Testing:** xUnit + Moq 4.20.72 + FluentAssertions 8.10.0
- **Language:** Arabic UI (resx), English code identifiers

## [SYSTEM_FLOW]
```
Login → Dashboard
            ├── Customer CRUD → Customer Profile → Owned Devices → Repair History
            ├── Device Registration → Duplicate Detection → Transfer Ownership
            ├── Repair Ticket → Status Progression → Photos → Warranty → Print
            ├── Search (by Name / IMEI) → Results with History
            └── Dashboard (Daily Stats + Overall Stats)
```

## [ARCHITECTURE]

```
MRMS.sln
├── MRMS.Core            Entities, Enums, Repository Interfaces (pure domain)
├── MRMS.Application     Services, DTOs, Service Interfaces (orchestrates business logic)
├── MRMS.Infrastructure  EF Core DbContext, Migrations, Repository impl, Seed Service
├── MRMS.UI              WPF Views, ViewModels, Arabic Resources, Navigation, Converters
└── MRMS.Tests           xUnit tests for Services (14 passing)
```

**Patterns:** Clean Architecture, MVVM, Repository, Unit of Work, Dependency Injection
**Data flow:** View → ViewModel → Service → Repository → DbContext → SQL Server

## [DOMAIN_MODEL]
```
User (Id, Username, PasswordHash, CreatedAt)

Customer (Id, FullName, PhoneNumber, CreatedAt)
  └── Device (Id, IMEI, Brand, Model, Color, CustomerId, CreatedAt)
        └── RepairTicket (Id, TicketNumber, DeviceId, ProblemDescription,
              ReceptionNotes, Status, ReceiveDate, ReceiveTime,
              RepairDescription, Cost, DeliveryDate, DeliveryTime, CreatedAt)
              ├── DevicePhoto (Id, RepairTicketId, FilePath, PhotoType [Before|After])
              └── Warranty (Id, RepairTicketId, DurationDays, ExpiryDate)
```

**TicketStatus:** Received, UnderInspection, WaitingForParts, InRepair, Repaired, Delivered, Cancelled

## [PROJECT_STRUCTURE]

### MRMS.Core/
- `Enums/TicketStatus.cs` — 7 status values
- `Entities/User.cs`, `Customer.cs`, `Device.cs`, `RepairTicket.cs`, `DevicePhoto.cs`, `Warranty.cs`
- `Interfaces/IRepository.cs` — generic CRUD
- `Interfaces/IUnitOfWork.cs` — aggregates all repositories

### MRMS.Infrastructure/
- `Data/AppDbContext.cs` — EF Core context with fluent config
- `Data/AppDbContextFactory.cs` — design-time factory for migrations
- `Migrations/20260526122907_InitialCreate.cs` — creates all 6 tables
- `Repositories/Repository.cs` — generic EF Core implementation
- `Repositories/UnitOfWork.cs` — wraps all repositories
- `Services/SeedService.cs` — seeds technician user (tech / admin123)
- `DependencyInjection.cs` — AddInfrastructure extension

### MRMS.Application/
- `DTOs/` — LoginDto, CustomerDto, DeviceDto, RepairTicketDto, WarrantyDto, DashboardStatsDto, SearchResultDto, PhotoDto
- `Interfaces/` — IAuthService, ICustomerService, IDeviceService, IRepairTicketService, ISearchService, IWarrantyService, IPhotoService
- `Services/` — all service implementations
- `DependencyInjection.cs` — AddApplication extension

### MRMS.UI/
- `App.xaml` + `App.xaml.cs` — DI container setup, startup with LoginWindow
- `Views/LoginWindow.xaml` — login with username/password
- `Views/MainWindow.xaml` — main shell hosting navigation Frame
- `Views/HomePage.xaml` — sidebar navigation (Dashboard, Customers, RegisterDevice, CreateRepairTicket, TransferDevice, Search, Logout)
- `Views/DashboardPage.xaml` — stat cards
- `Views/CustomerListPage.xaml` — DataGrid with add/edit/delete/view-profile
- `Views/CustomerFormPage.xaml` — add/edit customer
- `Views/CustomerProfilePage.xaml` — customer details + owned devices + repair history
- `Views/DeviceRegistrationPage.xaml` — register device with IMEI duplicate detection
- `Views/DeviceTransferPage.xaml` — transfer device ownership by IMEI
- `Views/RepairTicketCreatePage.xaml` — find device by IMEI, create ticket
- `Views/RepairTicketDetailPage.xaml` — ticket info, status update, repair info, photos, warranty, print
- `Views/SearchPage.xaml` — search by name/IMEI
- `ViewModels/` — LoginVM, DashboardVM, CustomerListVM, CustomerFormVM, DeviceRegistrationVM, DeviceTransferVM, RepairTicketVM, RepairTicketCreateVM, CustomerProfileVM, SearchVM
- `Converters/` — TicketStatusConverter, BoolToVisibilityConverter
- `Services/AppNavigationService.cs` — frame-based navigation with parameter passing
- `Resources/Strings.ar.resx` — Arabic UI strings

### MRMS.Tests/
- `Services/AuthServiceTests.cs` — 2 tests (valid/invalid login)
- `Services/CustomerServiceTests.cs` — 4 tests (create, delete, hasDevices)
- `Services/DeviceServiceTests.cs` — 4 tests (register, duplicate check, transfer)
- `Services/RepairTicketServiceTests.cs` — 4 tests (create, deliver, terminal-state, dashboard stats)
- **Total: 14 tests, 0 failures**

## [VERIFIABLE_GOALS_STATUS]
1. ✅ Foundation scaffold + DB migration creates all tables
2. ✅ Login with seeded technician account succeeds (service tested)
3. ✅ Full customer CRUD with delete protection (delete blocked if has devices)
4. ✅ Device registration with IMEI duplicate detection
5. ✅ Device ownership transfer preserves repair history
6. ✅ Repair ticket lifecycle (Received → ... → Delivered, terminal-state enforcement tested)
7. ✅ Auto-record delivery date/time on Delivered status
8. ✅ Photo attach/view (Before/After) — UI in RepairTicketDetailPage
9. ✅ Photo persistence after delivery — service handles it
10. ✅ Warranty assignment — UI in RepairTicketDetailPage
11. ✅ Partial search by customer name and IMEI
12. ✅ Dashboard displays stats (today received/delivered, in-repair, totals)
13. ✅ Print repair receipt — button in RepairTicketDetailPage
14. ✅ All UI strings in Arabic via resx resources
15. ✅ dotnet test passes (14/14)
16. ✅ Create repair ticket (find device by IMEI, problem description)
17. ✅ Repair ticket detail (status update, repair info, photos, warranty, print)
18. ✅ Device ownership transfer (find by IMEI, select new customer)
19. ✅ Customer profile (info + owned devices + repair history)

## [ORPHANS_AND_PENDING]
- No multi-user / role management (single technician, v1 scope)
- No database backup/restore mechanism (future)
- No CSV/Excel export
- No SMS/email notifications (out of scope)
- No dark mode / theme switching
- No pagination for large lists (v1 assumes moderate data volume)

## [HOW_TO_RUN]
1. Ensure SQL Server Express is running on `ABDO-DESKTOP\SQLEXPRESS`
2. Create database `MRMS` (or run migration)
3. `dotnet ef database update --project MRMS.Infrastructure --startup-project MRMS.UI`
4. `dotnet run --project MRMS.UI`
5. Login with: `tech` / `admin123`
