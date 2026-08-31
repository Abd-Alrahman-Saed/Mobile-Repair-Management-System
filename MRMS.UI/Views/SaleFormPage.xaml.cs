using System.Windows.Controls;
using System.Windows.Input;
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

        _vm.ItemScanned += () => QuantityBox.Focus();

        Loaded += async (_, _) =>
        {
            await vm.LoadCommand.ExecuteAsync(null);
            ScanBox.Focus();
        };
    }

    private void ScanBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.ScanItemCommand.Execute(null);
    }
}
