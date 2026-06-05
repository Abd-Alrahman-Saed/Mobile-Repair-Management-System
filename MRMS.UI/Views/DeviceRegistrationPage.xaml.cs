using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class DeviceRegistrationPage : Page
{
    private readonly DeviceRegistrationViewModel _vm;
    private readonly AppNavigationService _navigationService;
    private CancellationTokenSource? _customerSearchCts;

    public DeviceRegistrationPage(DeviceRegistrationViewModel vm, AppNavigationService navigationService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;
        _vm.SaveCompleted += () =>
        {
            if (_navigationService.CanGoBack)
                _navigationService.GoBack();
        };
    }

    private void Imei_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _vm.SearchImeiCommand.Execute(null);
        }
    }

    private async void CustomerNameSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _customerSearchCts?.Cancel();
        _customerSearchCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _customerSearchCts.Token);
            await _vm.SearchCustomerCommand.ExecuteAsync(null);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
