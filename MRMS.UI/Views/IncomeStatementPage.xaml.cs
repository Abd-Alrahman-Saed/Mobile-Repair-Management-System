using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class IncomeStatementPage : Page
{
    private readonly IncomeStatementViewModel _vm;

    public IncomeStatementPage(IncomeStatementViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);
    }
}
