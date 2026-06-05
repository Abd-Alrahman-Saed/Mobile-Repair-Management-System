using System.Windows.Controls;
using MRMS.Application.DTOs;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class CustomerListPage : Page
{
    private readonly CustomerListViewModel _vm;

    public CustomerListPage(CustomerListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void CustomersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CustomersGrid.SelectedItem is CustomerDto customer)
            _vm.ViewProfileCommand.Execute(customer.Id);
    }
}
