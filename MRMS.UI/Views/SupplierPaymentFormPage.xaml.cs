using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class SupplierPaymentFormPage : Page
{
    private readonly SupplierPaymentFormViewModel _vm;

    public SupplierPaymentFormPage(SupplierPaymentFormViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
