using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class RepairTicketCreatePage : Page
{
    private readonly RepairTicketCreateViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public RepairTicketCreatePage(RepairTicketCreateViewModel vm, AppNavigationService navigationService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;
        _vm.TicketCreated += ticketId =>
        {
            var detailPage = App.ServiceProvider!.GetRequiredService<RepairTicketDetailPage>();
            _navigationService.NavigateTo(detailPage, "TicketId", ticketId);
        };
    }
}
