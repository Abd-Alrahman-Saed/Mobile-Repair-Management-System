# Contributing to MRMS

Thank you for your interest in contributing to **MRMS (Mobile Repair Management System)**! We welcome contributions, bug reports, and suggestions to make MRMS the premier open-source ERP and workshop management platform for electronics repair businesses.

---

## 🛠️ Development Setup

### Prerequisites
- **Operating System:** Windows 10 (Build 19041+) or Windows 11
- **SDK:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`)
- **IDE:** Visual Studio 2026 (with *.NET Desktop Development* workload) or Visual Studio Code / JetBrains Rider with C# Dev Kit
- **Database (Optional for Advanced Mode):** Microsoft SQL Server 2022+ / SQL Server Express (Default fallback is SQLite)

### Build & Run
1. Clone the repository:
   ```powershell
   git clone https://github.com/Abd-Alrahman-Saed/Mobile-Repair-Management-System---MRMS.git
   cd "Mobile-Repair-Management-System---MRMS"
   ```
2. Restore dependencies:
   ```powershell
   dotnet restore MRMS.slnx
   ```
3. Run test suite to verify everything compiles cleanly:
   ```powershell
   dotnet test MRMS.Tests/MRMS.Tests.csproj
   ```
4. Launch the application:
   ```powershell
   dotnet run --project MRMS.UI/MRMS.UI.csproj
   ```

---

## 🏛️ Architectural Guidelines

MRMS follows a strict **Clean Architecture** layered design:

1. **`MRMS.Core` (Domain Layer)**
   - Contains Domain Entities, Enums, and Repository/Unit-of-Work Interfaces.
   - **Zero external dependencies** (no EF Core, no UI libraries, no external services).
   - Entities must remain focused on domain invariants.

2. **`MRMS.Application` (Business Logic Layer)**
   - Contains Services, DTOs, Service Interfaces, and Validation.
   - Depends only on `MRMS.Core`.
   - Business operations (intake validation, POS checkout, cash session balancing, stock adjustment) reside here.

3. **`MRMS.Infrastructure` (Data & External Services Layer)**
   - Contains `AppDbContext`, EF Core Fluent Configurations, Migrations, Repository Implementations, and Seeding.
   - Dual-engine support: SQL Server and SQLite.
   - Depends on `MRMS.Application` and `MRMS.Core`.

4. **`MRMS.UI` (Presentation Layer)**
   - WPF desktop client using **MVVM** pattern powered by `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`).
   - Native RTL Arabic UI support with English resource localization.
   - Depends on `MRMS.Application` and `MRMS.Infrastructure` (Composition Root).

5. **`MRMS.Tests` (Test Suite)**
   - xUnit, Moq, and FluentAssertions.
   - Unit tests must be fast, deterministic, and mock external dependencies via `IUnitOfWork`.

---

## 🌿 Branching & Commit Workflow

- **Branch Naming:**
  - `feat/feature-name` for new features
  - `fix/bug-description` for bug fixes
  - `refactor/subsystem-name` for code refactoring
  - `docs/topic-name` for documentation improvements
- **Commit Messages:** Follow standard Conventional Commits:
  - `feat(pos): add multi-line invoice discount validation`
  - `fix(inventory): prevent negative stock on service item returns`
  - `docs(readme): expand architecture and setup instructions`

---

## 🧪 Testing Standards

- All new business logic must include unit tests in `MRMS.Tests`.
- Ensure all 128+ existing tests pass prior to submitting a pull request:
  ```powershell
  dotnet test
  ```
- No compiler warnings or broken migrations.

---

## 📄 License

By contributing to MRMS, you agree that your contributions will be licensed under the project's [MIT License](LICENSE).
