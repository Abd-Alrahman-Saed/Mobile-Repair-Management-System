using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class SuppliersPage : Page
{
    private readonly SuppliersViewModel _vm;

    public SuppliersPage(SuppliersViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        if (!vm.CanViewBalance)
            foreach (var col in new[] { OpeningBalanceColumn, TotalPurchasesColumn, TotalPaymentsColumn, OutstandingBalanceColumn })
                col.Visibility = System.Windows.Visibility.Collapsed;

        Loaded += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);
    }
}
