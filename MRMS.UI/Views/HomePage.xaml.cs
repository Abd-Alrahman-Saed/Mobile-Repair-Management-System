using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
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

    private void Payables_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<PayablesPage>());

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
        var login = App.ServiceProvider!.GetRequiredService<LoginWindow>();
        login.Show();
        System.Windows.Window.GetWindow(this)?.Close();
    }
}
