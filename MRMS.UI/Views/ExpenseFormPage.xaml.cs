using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class ExpenseFormPage : Page
{
    private readonly ExpenseFormViewModel _vm;

    public ExpenseFormPage(ExpenseFormViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }
}
