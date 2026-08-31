using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
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

    private void AddNewItem_Click(object sender, RoutedEventArgs e)
    {
        var window = App.ServiceProvider!.GetRequiredService<QuickAddItemWindow>();
        window.Owner = Window.GetWindow(this);

        if (window.ShowDialog() == true && window.CreatedItem is not null)
        {
            _vm.AddAndSelectNewItem(window.CreatedItem);
        }
    }

    private void EditPrices_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedItemId <= 0)
        {
            _vm.ErrorMessage = "يرجى اختيار الصنف أولاً";
            return;
        }

        var window = App.ServiceProvider!.GetRequiredService<QuickPriceEditWindow>();
        window.Owner = Window.GetWindow(this);
        window.LoadItem(_vm.Items.FirstOrDefault(i => i.Id == _vm.SelectedItemId)!);

        if (window.ShowDialog() == true)
        {
            _vm.ApplyPriceUpdate(_vm.SelectedItemId, window.SavedCostPrice, window.SavedSellingPrice);
        }
    }
}
