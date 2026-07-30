using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application;
using MRMS.Infrastructure;
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
            args.Handled = true;
        };

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/mrms-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Application starting...");

        var services = new ServiceCollection();

        services.AddInfrastructure(
            "Server=ABDO-DESKTOP\\SQLEXPRESS;Database=MRMS;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=5;");

        services.AddApplication();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CustomerListViewModel>();
        services.AddTransient<CustomerFormViewModel>();
        services.AddTransient<DeviceRegistrationViewModel>();
        services.AddTransient<DeviceTransferViewModel>();
        services.AddTransient<RepairTicketViewModel>();
        services.AddTransient<RepairTicketCreateViewModel>();
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
        services.AddTransient<PayablesViewModel>();
        services.AddTransient<IncomeStatementViewModel>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
        services.AddTransient<HomePage>();
        services.AddTransient<DashboardPage>();
        services.AddTransient<CustomerListPage>();
        services.AddTransient<CustomerFormPage>();
        services.AddTransient<DeviceRegistrationPage>();
        services.AddTransient<DeviceTransferPage>();
        services.AddTransient<RepairTicketCreatePage>();
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
        services.AddTransient<PayablesPage>();
        services.AddTransient<IncomeStatementPage>();

        services.AddSingleton<AppNavigationService>();
        services.AddSingleton<NotificationService>();

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

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
