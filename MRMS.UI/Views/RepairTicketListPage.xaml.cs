using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class RepairTicketListPage : Page
{
    private readonly RepairTicketListViewModel _vm;

    public RepairTicketListPage(RepairTicketListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (NavigationService is not null)
            NavigationService.Navigated += OnNavigated;
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (NavigationService is not null)
            NavigationService.Navigated -= OnNavigated;
    }

    private async void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (e.Content is RepairTicketListPage)
            await _vm.LoadCommand.ExecuteAsync(null);
    }
}
