using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public DashboardPage(DashboardViewModel vm, AppNavigationService navigationService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void RecentTicketsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RecentTicketsGrid.SelectedItem is RepairTicketDto ticket && ticket.Id > 0)
        {
            var page = App.ServiceProvider!.GetRequiredService<RepairTicketDetailPage>();
            _navigationService.NavigateTo(page, "TicketId", ticket.Id);
        }
    }
}
