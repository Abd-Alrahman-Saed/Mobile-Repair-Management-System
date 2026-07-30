using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class SaleFormPage : Page
{
    private readonly SaleFormViewModel _vm;

    public SaleFormPage(SaleFormViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
