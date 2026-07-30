using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class PurchaseFormPage : Page
{
    private readonly PurchaseFormViewModel _vm;

    public PurchaseFormPage(PurchaseFormViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
