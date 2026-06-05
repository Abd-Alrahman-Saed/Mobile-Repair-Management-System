using System.Windows.Controls;
using System.Windows.Input;
using MRMS.Application.DTOs;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class CustomerProfilePage : Page
{
    private readonly CustomerProfileViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public CustomerProfilePage(CustomerProfileViewModel vm, AppNavigationService navigationService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;
        Loaded += async (_, _) =>
        {
            var customerId = _navigationService.GetParameter<int?>("CustomerId");
            if (customerId.HasValue)
                await _vm.LoadAsync(customerId.Value);
        };
    }

    private void TicketsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TicketsGrid.SelectedItem is RepairTicketDto ticket && ticket.Id > 0)
        {
            _vm.ViewTicketDetailCommand.Execute(ticket.Id);
        }
    }
}
