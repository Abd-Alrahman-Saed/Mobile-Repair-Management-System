using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class StockMovementFormPage : Page
{
    private readonly StockMovementFormViewModel _vm;

    public StockMovementFormPage(StockMovementFormViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
