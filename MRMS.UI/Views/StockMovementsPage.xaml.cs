using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class StockMovementsPage : Page
{
    private readonly StockMovementsViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public StockMovementsPage(StockMovementsViewModel vm, AppNavigationService navigationService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }

    private void AddMovement_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var page = App.ServiceProvider!.GetRequiredService<StockMovementFormPage>();
        _navigationService.NavigateTo(page);
    }
}
