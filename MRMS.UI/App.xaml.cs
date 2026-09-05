using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application;
using MRMS.Infrastructure;
using MRMS.Infrastructure.Configuration;
using MRMS.Infrastructure.Services;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;
using MRMS.UI.Views;
using Serilog;

namespace MRMS.UI;

public partial class App : System.Windows.Application
{
    public static ServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled exception");
            MessageBox.Show(
                "حدث خطأ غير متوقّع. تم تسجيل التفاصيل في ملف السجل.",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/mrms-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Application starting...");

        var services = new ServiceCollection();

        // Database provider + connection string live in %AppData%\MRMS\connection-config.json, not baked
        // into the app, so they survive updates and can differ per machine without rebuilding. A machine
        // that already has this config (e.g. this dev machine, pointed at SQL Server) keeps working as-is;
        // a brand-new machine with no config yet bootstraps a self-contained local SQLite database with no
        // external server required. See ConnectionStringProvider for details.
        var databaseConfig = ConnectionStringProvider.GetDatabaseConfig();
        services.AddInfrastructure(databaseConfig);

        services.AddApplication();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<UserFormViewModel>();
        services.AddTransient<AuditLogViewModel>();
        services.AddTransient<OwnerApprovalViewModel>();
        services.AddTransient<ShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CustomerListViewModel>();
        services.AddTransient<CustomerFormViewModel>();
        services.AddTransient<DeviceRegistrationViewModel>();
        services.AddTransient<DeviceTransferViewModel>();
        services.AddTransient<RepairTicketViewModel>();
        services.AddTransient<RepairTicketListViewModel>();
        services.AddTransient<CustomerProfileViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<ItemsViewModel>();
        services.AddTransient<ItemFormViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<SupplierFormViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<PurchasesViewModel>();
        services.AddTransient<PurchaseFormViewModel>();
        services.AddTransient<SalesViewModel>();
        services.AddTransient<SaleFormViewModel>();
        services.AddTransient<ExpensesViewModel>();
        services.AddTransient<ExpenseFormViewModel>();
        services.AddTransient<SupplierPaymentsViewModel>();
        services.AddTransient<SupplierPaymentFormViewModel>();
        services.AddTransient<StockMovementsViewModel>();
        services.AddTransient<StockMovementFormViewModel>();
        services.AddTransient<StockBalanceViewModel>();
        services.AddTransient<CashbookViewModel>();
        services.AddTransient<IncomeStatementViewModel>();
        services.AddTransient<MonthlyReportViewModel>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<ChangePasswordWindow>();
        services.AddTransient<UsersPage>();
        services.AddTransient<UserFormPage>();
        services.AddTransient<AuditLogPage>();
        services.AddTransient<OwnerApprovalWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<HomePage>();
        services.AddTransient<DashboardPage>();
        services.AddTransient<CustomerListPage>();
        services.AddTransient<CustomerFormPage>();
        services.AddTransient<DeviceRegistrationPage>();
        services.AddTransient<DeviceTransferPage>();
        services.AddTransient<RepairTicketDetailPage>();
        services.AddTransient<RepairTicketListPage>();
        services.AddTransient<CustomerProfilePage>();
        services.AddTransient<SearchPage>();
        services.AddTransient<ItemsPage>();
        services.AddTransient<ItemFormPage>();
        services.AddTransient<SuppliersPage>();
        services.AddTransient<SupplierFormPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<PurchasesPage>();
        services.AddTransient<PurchaseFormPage>();
        services.AddTransient<QuickAddItemWindow>();
        services.AddTransient<QuickPriceEditWindow>();
        services.AddTransient<SalesPage>();
        services.AddTransient<SaleFormPage>();
        services.AddTransient<ExpensesPage>();
        services.AddTransient<ExpenseFormPage>();
        services.AddTransient<SupplierPaymentsPage>();
        services.AddTransient<SupplierPaymentFormPage>();
        services.AddTransient<StockMovementsPage>();
        services.AddTransient<StockMovementFormPage>();
        services.AddTransient<StockBalancePage>();
        services.AddTransient<CashbookPage>();
        services.AddTransient<IncomeStatementPage>();
        services.AddTransient<MonthlyReportPage>();

        services.AddSingleton<AppNavigationService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<BackupSchedulerService>();

        ServiceProvider = services.BuildServiceProvider();

        // Seed database in background
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
                await seedService.SeedAsync();
                Log.Information("Database seeded successfully");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Database seeding failed (non-fatal)");
            }
        });

        var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
