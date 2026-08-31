# MRMS — Mobile Repair Management System

MRMS is a Windows desktop application for managing a mobile phone repair business — tracking repair tickets, customers, inventory, and business analytics from a single, native UI.

## Features

- **Repair & customer tracking** — manage repair jobs and customer records from one place
- **Analytics dashboard** — built-in charts and reporting powered by LiveCharts
- **Local & networked data storage** — Entity Framework Core with SQL Server and SQLite providers
- **Structured logging** — application activity logged via Serilog for diagnostics and auditing
- **Multi-language UI** — interface resources included for Arabic, English, Czech, German, Spanish, French, Italian, Japanese, Korean, Polish, Portuguese (Brazil), Russian, Turkish, and Chinese (Simplified & Traditional)

## Tech Stack

| Layer | Technology |
|---|---|
| UI | WPF (.NET 10, `net10.0`) |
| Architecture | Layered — `MRMS.Core`, `MRMS.Application`, `MRMS.Infrastructure`, `MRMS.UI` |
| MVVM | CommunityToolkit.Mvvm |
| Data Access | Entity Framework Core (SQL Server / SQLite) |
| Charts | LiveChartsCore (SkiaSharp) |
| Logging | Serilog |

This release is published as a **self-contained deployment** — it bundles the .NET 10 runtime, so end users do not need to install .NET separately to run it.

## Prerequisites

- Windows 10 or later (64-bit or 32-bit)
- [Git](https://git-scm.com/downloads) installed and available on your `PATH`

## Installation (CLI)

Open PowerShell (or your terminal of choice) and run:

```powershell
# 1. Clone the repository
git clone https://github.com/Abd-Alrahman-Saed/Mobile-Repair-Management-System---MRMS.git

# 2. Enter the project directory
cd "Mobile-Repair-Management-System---MRMS"

# 3. Launch the application
.\MRMS.UI.exe
```

That's it — no build step or separate runtime installation is required, since the release is self-contained.

### Alternative: run via the .NET CLI

If you prefer launching through `dotnet` instead of the native executable:

```powershell
dotnet MRMS.UI.dll
```

## Updating

To pull the latest release into an existing local clone:

```powershell
cd "Mobile-Repair-Management-System---MRMS"
git pull origin main
```

## Project Structure

```
Mobile-Repair-Management-System---MRMS/
├── MRMS.UI.exe              # Application entry point
├── MRMS.UI.dll               # UI layer (WPF, views, view models)
├── MRMS.Application.dll      # Application/business logic layer
├── MRMS.Infrastructure.dll   # Data access & external integrations
├── MRMS.Core.dll             # Domain models & core abstractions
├── ar/, de/, fr/, ...         # Localized UI resource assemblies
└── logs/                      # Runtime log output (not tracked in git)
```

## License

No license has been specified for this project yet. All rights are reserved by the author unless a license is added.

## Author

**Abd Alrahman Saed**
