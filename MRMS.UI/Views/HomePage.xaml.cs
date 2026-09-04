using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Controls;
using MRMS.UI.Services;

namespace MRMS.UI.Views;

public partial class HomePage : Page
{
    private readonly AppNavigationService _navigationService;

    public HomePage(AppNavigationService navigationService)
    {
        InitializeComponent();
        _navigationService = navigationService;
        _navigationService.Initialize(ContentFrame);

        var notificationService = App.ServiceProvider!.GetRequiredService<NotificationService>();
        var notification = new NotificationControl(notificationService);
        NotificationLayer.Children.Add(notification);

        App.ServiceProvider!.GetRequiredService<BackupSchedulerService>().Start();

        var permissions = App.ServiceProvider!.GetRequiredService<IPermissionService>();
        var canManageUsers = permissions.Can(Permission.ManageUsers);
        var canViewAudit = permissions.Can(Permission.ViewAuditLog);
        UsersNavButton.Visibility = canManageUsers ? Visibility.Visible : Visibility.Collapsed;
        AuditLogNavButton.Visibility = canViewAudit ? Visibility.Visible : Visibility.Collapsed;
        AdminSectionHeader.Visibility = (canManageUsers || canViewAudit) ? Visibility.Visible : Visibility.Collapsed;

        Loaded += (_, _) => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<DashboardPage>());
    }

    private void Back_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _navigationService.GoBack();
    }

    private void ContentFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
    }

    private void Dashboard_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<DashboardPage>());

    private void Users_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<UsersPage>());

    private void AuditLog_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<AuditLogPage>());

    private void Customers_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<CustomerListPage>());

    private void RegisterDevice_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<DeviceRegistrationPage>());

    private void RepairTickets_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<RepairTicketListPage>());

    private void CreateRepairTicket_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<RepairTicketCreatePage>());

    private void TransferDevice_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<DeviceTransferPage>());

    private void Purchases_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<PurchasesPage>());

    private void Sales_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<SalesPage>());

    private void SupplierPayments_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<SupplierPaymentsPage>());

    private void StockMovements_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<StockMovementsPage>());

    private void Expenses_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<ExpensesPage>());

    private void StockBalance_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<StockBalancePage>());

    private void Cashbook_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<CashbookPage>());

    private void IncomeStatement_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<IncomeStatementPage>());

    private void MonthlyReport_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<MonthlyReportPage>());

    private void Items_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<ItemsPage>());

    private void Suppliers_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<SuppliersPage>());

    private void Settings_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<SettingsPage>());

    private void Search_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<SearchPage>());

    private void Logout_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        App.ServiceProvider!.GetRequiredService<MRMS.Application.Interfaces.ICurrentUserService>().Clear();
        var login = App.ServiceProvider!.GetRequiredService<LoginWindow>();
        login.Show();
        System.Windows.Window.GetWindow(this)?.Close();
    }

    private void Page_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F5)
        {
            var page = ContentFrame.Content as System.Windows.Controls.Page;
            if (page?.DataContext is CommunityToolkit.Mvvm.Input.IAsyncRelayCommand loadCmd)
                _ = loadCmd.ExecuteAsync(null);
            else
            {
                var reloadMethod = page?.GetType().GetMethod("LoadAsync");
                reloadMethod?.Invoke(page?.DataContext, null);
            }
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.F2)
        {
            _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<SearchPage>());
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            var currentType = ContentFrame.Content?.GetType();
            if (currentType == typeof(PurchasesPage))
                _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<PurchaseFormPage>());
            else if (currentType == typeof(SalesPage))
                _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<SaleFormPage>());
            else if (currentType == typeof(ExpensesPage))
                _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<ExpenseFormPage>());
            else if (currentType == typeof(ItemsPage))
                _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<ItemFormPage>());
            else if (currentType == typeof(SuppliersPage))
                _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<SupplierFormPage>());
            else if (currentType == typeof(SupplierPaymentsPage))
                _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<SupplierPaymentFormPage>());
            else if (currentType == typeof(StockMovementsPage))
                _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<StockMovementFormPage>());
            e.Handled = true;
        }
    }
}
