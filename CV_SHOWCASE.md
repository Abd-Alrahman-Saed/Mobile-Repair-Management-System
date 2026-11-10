# 💼 MRMS — CV, Portfolio & Interview Showcase Guide

This guide is designed to help you showcase **MRMS (Mobile Repair Management System)** on your **CV / Resume**, **LinkedIn Profile**, **Portfolio Website**, and in **Technical Interviews**.

---

## 📄 1. Resume Bullet Points (Ready to Copy & Paste)

Choose the version that best matches your target role and resume layout. All bullet points follow Google's recommended **X-Y-Z formula**:  
*“Accomplished [X] as measured by [Y], by doing [Z]”*

---

### Option A: Standard 1-Page Resume (3 High-Impact Bullet Points)

> **Mobile Repair Management System (MRMS) | Enterprise ERP & POS Desktop App**  
> *.NET 10, C# 13, WPF, Clean Architecture, EF Core, SQL Server, SQLite, CommunityToolkit.Mvvm, xUnit*
> * Architected and developed a full-cycle ERP, POS, and workshop management desktop system using **.NET 10** and **Clean Architecture**, supporting 22 relational domain entities and 27 business services across workshop repairs, inventory, procurement, and accounting.
> * Implemented high-performance MVVM presentation using **CommunityToolkit.Mvvm** source generators and designed a dual-engine data access layer supporting both **SQL Server** (networked multi-user) and **SQLite** (portable embedded).
> * Built a cash register shift engine with blind drop reconciliation, automated double-entry accounting ledgers, and engineered an automated test suite of **128 xUnit tests** achieving 100% test pass rate across business rules and edge cases.

---

### Option B: Detailed 2-Page Resume / Senior .NET Developer (6-8 Bullet Points)

> **Mobile Repair Management System (MRMS) | Full-Cycle ERP & Workshop Management System**  
> *Tech Stack: .NET 10 LTS, C# 13, WPF, XAML, EF Core 10, SQL Server 2025, SQLite, LiveChartsCore, Serilog, xUnit, Moq, FluentAssertions*
> * **Enterprise Architecture:** Engineered an enterprise desktop ERP following strict **Clean Architecture (Onion Architecture)**, cleanly decoupling the pure domain model (`MRMS.Core`), business orchestration (`MRMS.Application`), data access (`MRMS.Infrastructure`), and modern WPF UI (`MRMS.UI`).
> * **Workshop & Hardware Lifecycle:** Designed a 7-stage service state machine (`Received` ➔ `Delivered`) featuring single-screen device intake, hardware IMEI duplicate detection, ownership transfer history, technician dispatch, before/after photo documentation, and automated warranty tracking.
> * **POS & Accounts Receivable:** Built a high-speed Point-of-Sale (POS) invoicing engine handling multi-line billing, barcode scanning, customer debt tracking, partial installment settlements, and gross profit margin protection.
> * **Cash Session & Drawer Reconciliation:** Engineered a POS cash register module managing shift opening floats, mid-shift payouts, and blind-count reconciliation to automatically detect cashier variances (overage/shortage) with immutable daily Z-reports.
> * **Inventory Valuation & Warehousing:** Implemented a multi-tier inventory engine handling retail accessories, repair spare parts, and non-stockable labor services with automated stock movement auditing, low-stock reorder thresholds, and physical stock count reconciliation.
> * **Financial Intelligence & Analytics:** Developed automated double-entry journal postings, general ledger, trial balance, and real-time Income Statement (P&L), integrated with **LiveChartsCore (SkiaSharp)** for interactive financial trends.
> * **Dual Database Strategy & Reliability:** Designed a runtime database abstraction enabling seamless operation on **Microsoft SQL Server** for multi-terminal shop networks or **SQLite** for zero-configuration single-machine deployments.
> * **Quality Assurance:** Authored **128 unit tests** using **xUnit**, **Moq**, and **FluentAssertions**, testing state machine transitions, concurrent stock decrements, and financial validation logic with zero defects.

---

## 🌐 2. LinkedIn Featured Project & Summary

### Project Title
**MRMS — Enterprise ERP, Point-of-Sale & Electronics Workshop Management System**

### Tagline
*Full-cycle desktop ERP & POS engineered with .NET 10, WPF, Clean Architecture, and EF Core.*

### Project Description
> Developed **MRMS**, an enterprise desktop ERP and workshop management platform built from the ground up for mobile device service centers and electronics retailers.
>
> **Key Engineering Highlights:**
> 🔹 **Clean Architecture:** Strict separation between pure domain logic, application use cases, EF Core data access, and WPF presentation.  
> 🔹 **Modern WPF & MVVM:** Built with CommunityToolkit.Mvvm source generators, reducing boilerplate code by 70% while maximizing UI responsiveness.  
> 🔹 **Enterprise Features:** Single-screen device intake with IMEI validation, 7-stage repair Kanban board, multi-line POS billing with customer debt (AR), cash drawer shift balancing (blind count reconciliation), inventory valuation, supplier payables (AP), and double-entry accounting (P&L / Trial Balance).  
> 🔹 **Dual Database Engine:** Dynamic switching between Microsoft SQL Server (networked workstations) and SQLite (portable standalone).  
> 🔹 **Test-Driven Rigor:** 128 automated unit tests verifying business invariants, state transitions, and financial calculations.  
>
> 🔗 GitHub: https://github.com/Abd-Alrahman-Saed/Mobile-Repair-Management-System---MRMS

### Skills to Tag on LinkedIn
`.NET Framework / .NET Core`, `C#`, `WPF (Windows Presentation Foundation)`, `Software Architecture`, `Entity Framework (EF Core)`, `Microsoft SQL Server`, `SQLite`, `Object-Oriented Programming (OOP)`, `Unit Testing`, `Clean Architecture`.

---

## 🏷️ 3. GitHub Repository Settings (To Set in GitHub UI)

### Repository About Description
> 📱 Enterprise Desktop ERP, Point-of-Sale (POS) & Workshop Management System built with .NET 10, WPF, Clean Architecture, EF Core, and CommunityToolkit.Mvvm. Features 7-stage repair lifecycle, IMEI tracking, cash register shifts, inventory valuation, supplier payables, and 128 unit tests.

### Recommended GitHub Topics (Tags)
```text
csharp, dotnet-10, wpf, clean-architecture, entity-framework-core, erp, pos, point-of-sale, inventory-management, repair-management, cash-register, livecharts, mvvm, communitytoolkit-mvvm, serilog, xunit, sqlite, sqlserver, desktop-app, arabic-support
```

---

## 🎤 4. Technical Interview Preparation & Cheat Sheet

When an interviewer asks: **“Tell me about the most technically challenging project you’ve worked on,”** use this structured response.

### 60-Second Elevator Pitch
> *"I built MRMS, an enterprise-grade ERP and Point-of-Sale desktop application tailored for mobile phone repair centers and electronics retailers using .NET 10 and WPF. The system manages the entire operational lifecycle: from hardware intake with IMEI duplicate prevention and 7-stage repair tracking, to multi-line POS billing, cashier shift balancing with blind cash drops, supplier payables, and automated double-entry accounting.
> 
> Architecturally, I implemented Clean Architecture with 22 domain entities and 27 services, utilized CommunityToolkit.Mvvm source generators to eliminate boilerplate, supported dual database providers (SQL Server and SQLite), and ensured robust code quality with a 128-test automated test suite."*

---

### Deep-Dive Interview Questions & Winning Answers

#### Q1: Why did you choose Clean Architecture instead of a standard 3-tier architecture?
> **Answer:**  
> *"In a standard 3-tier architecture, the UI and business logic often become tightly coupled to the data access layer and ORM entities. In MRMS, business rules around hardware repair state machines, warranty validity, and cash drawer variance calculations are critical and must not depend on database or UI details.
> 
> With Clean Architecture:
> 1. `MRMS.Core` is pure POCO entities and abstractions with zero external dependencies.
> 2. `MRMS.Application` contains all business orchestration and can be tested in isolation using mocks without spinning up a database.
> 3. `MRMS.Infrastructure` handles EF Core mapping, migrations, and database engine differences.
> This enabled us to easily add dual database support (SQL Server and SQLite) without altering a single line of business logic or UI code."*

#### Q2: How did you handle EF Core DbContext lifetime in WPF?
> **Answer:**  
> *"In ASP.NET Core, `DbContext` is typically scoped to an incoming HTTP request. In WPF, there is no built-in request scope. Registering `DbContext` as `Transient` creates a severe bug where multi-service operations (like a sale that decrements stock, records customer debt, and posts a journal entry) each receive independent DbContext instances, causing transaction desynchronization and uncommitted changes.
> 
> To solve this, I registered `AppDbContext` and `IUnitOfWork` as `Scoped`, allowing services in the resolution tree to share a single change tracker. Furthermore, for background tasks (such as database seeding and scheduled backups), I explicitly create an `IServiceScope` so background operations execute in their own isolated unit of work without conflicting with the UI thread."*

#### Q3: How does the Cash Drawer (CashSession) shift reconciliation work?
> **Answer:**  
> *"A major issue in retail and repair shops is theft and cashier accounting errors. I designed a formal CashSession lifecycle:
> 1. At shift start, the cashier records the opening float.
> 2. During the shift, the system tracks cash intake from repairs and POS sales, as well as payouts for expenses and supplier installments.
> 3. At closing, the cashier performs a **blind physical cash drop** (entering what they counted in the drawer without seeing the system's expected total).
> 4. The application automatically calculates `Discrepancy = Actual - Expected`, flagging overages or shortages, and locks the shift into an immutable Z-report for the shop owner."*

#### Q4: Why CommunityToolkit.Mvvm instead of Prism or traditional INotifyPropertyChanged?
> **Answer:**  
> *"Traditional WPF MVVM requires excessive boilerplate: backing fields, `OnPropertyChanged` invocations, and manual `RelayCommand` wiring. Older frameworks like Prism carry significant runtime overhead.
> 
> By leveraging `CommunityToolkit.Mvvm 8.4`, I utilized Roslyn compile-time source generators (`[ObservableProperty]`, `[RelayCommand]`). This generated clean, high-performance property notification code at compile time, eliminating reflection overhead and reducing codebase verbosity by roughly 70%."*

#### Q5: How did you approach testing, and what is covered in your 128 tests?
> **Answer:**  
> *"I designed unit tests using `xUnit`, `Moq`, and `FluentAssertions`. The tests focus on high-risk business logic:
> * Ensuring repair tickets cannot skip mandatory lifecycle states (e.g. from `Received` directly to `Delivered`).
> * Validating that stock cannot drop below zero for physical items, while allowing intangible service items to pass through without inventory constraints.
> * Verifying partial debt calculations on sales invoices and supplier statements.
> * Testing password hashing, salt validation, and role-based authorization rules.
> All 128 tests run in just over 2 seconds, ensuring fast feedback in the development loop."*

---

## 🏆 5. Summary of Key Metrics

| Metric | Value |
|---|---|
| **Solution Projects** | 5 (`Core`, `Application`, `Infrastructure`, `UI`, `Tests`) |
| **Domain Entities** | 22 Relational Entities |
| **Domain Enums** | 6 Business Enums |
| **Application Services** | 27 Dedicated Feature Services |
| **UI Views / Pages** | 35+ WPF Windows, Pages, Dialogs, and Print Previews |
| **Automated Unit Tests** | 128 Passing Unit Tests (0 failures) |
| **Compiler Warnings / Errors** | 0 Warnings, 0 Errors on .NET 10 LTS |
| **Supported Databases** | Microsoft SQL Server 2022+ & SQLite |
