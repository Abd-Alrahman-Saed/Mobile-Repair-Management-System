using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class MonthlyReportPage : Page
{
    private readonly MonthlyReportViewModel _vm;

    public MonthlyReportPage(MonthlyReportViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);
    }
}
