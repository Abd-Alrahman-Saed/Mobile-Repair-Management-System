using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.UI.Controls;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class HomePage : Page
{
    private readonly AppNavigationService _navigationService;
    private readonly ShellViewModel _shell;

    public HomePage(AppNavigationService navigationService, ShellViewModel shell)
    {
        InitializeComponent();
        _navigationService = navigationService;
        _shell = shell;
        DataContext = shell;

        _navigationService.Initialize(ContentFrame);
        _shell.LogoutRequested += OnLogoutRequested;

        var notificationService = App.ServiceProvider!.GetRequiredService<NotificationService>();
        NotificationLayer.Children.Add(new NotificationControl(notificationService));

        App.ServiceProvider!.GetRequiredService<BackupSchedulerService>().Start();

        Loaded += async (_, _) =>
        {
            _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<DashboardPage>());
            await _shell.RefreshAlertsCommand.ExecuteAsync(null);
        };
    }

    private void OnLogoutRequested()
    {
        var login = App.ServiceProvider!.GetRequiredService<LoginWindow>();
        login.Show();
        Window.GetWindow(this)?.Close();
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _navigationService.GoBack();

    private async void ContentFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        await _shell.RefreshAlertsCommand.ExecuteAsync(null);
    }

    private void SearchResult_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SearchResultDto result })
            _shell.OpenSearchResultCommand.Execute(result);
    }

    private void Page_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            var page = ContentFrame.Content as Page;
            if (page?.DataContext is CommunityToolkit.Mvvm.Input.IAsyncRelayCommand loadCmd)
                _ = loadCmd.ExecuteAsync(null);
            else
                page?.GetType().GetMethod("LoadAsync")?.Invoke(page.DataContext, null);
            e.Handled = true;
        }
        else if (e.Key == Key.F2)
        {
            SearchBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            var currentType = ContentFrame.Content?.GetType();
            var formType =
                currentType == typeof(PurchasesPage) ? typeof(PurchaseFormPage) :
                currentType == typeof(SalesPage) ? typeof(SaleFormPage) :
                currentType == typeof(ExpensesPage) ? typeof(ExpenseFormPage) :
                currentType == typeof(ItemsPage) ? typeof(ItemFormPage) :
                currentType == typeof(SuppliersPage) ? typeof(SupplierFormPage) :
                currentType == typeof(SupplierPaymentsPage) ? typeof(SupplierPaymentFormPage) :
                currentType == typeof(StockMovementsPage) ? typeof(StockMovementFormPage) :
                currentType == typeof(UsersPage) ? typeof(UserFormPage) : null;
            if (formType is not null)
                _navigationService.NavigateTo((Page)App.ServiceProvider!.GetRequiredService(formType));
            e.Handled = true;
        }
    }
}
