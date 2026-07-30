using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class SalesPage : Page
{
    private readonly SalesViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public SalesPage(SalesViewModel vm, AppNavigationService navigationService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;

        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }

    private void AddSale_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var page = App.ServiceProvider!.GetRequiredService<SaleFormPage>();
        _navigationService.NavigateTo(page);
    }
}
