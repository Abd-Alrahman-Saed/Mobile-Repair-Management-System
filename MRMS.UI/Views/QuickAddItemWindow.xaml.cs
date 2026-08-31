using System.Windows;
using System.Windows.Controls;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;

namespace MRMS.UI.Views;

public partial class QuickAddItemWindow : Window
{
    private readonly IItemService _itemService;

    public ItemDto? CreatedItem { get; private set; }

    public QuickAddItemWindow(IItemService itemService)
    {
        InitializeComponent();
        _itemService = itemService;

        Loaded += (_, _) => CodeBox.Focus();
    }

    private void CodeBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            NameBox.Focus();
            e.Handled = true;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorBorder.Visibility = Visibility.Collapsed;

        var code = CodeBox.Text.Trim();
        var name = NameBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            ShowError("كود الصنف مطلوب");
            return;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("اسم الصنف مطلوب");
            return;
        }
        if (!decimal.TryParse(CostPriceBox.Text, out var costPrice) || costPrice < 0)
        {
            ShowError("تكلفة الشراء غير صحيحة");
            return;
        }
        if (!decimal.TryParse(SellingPriceBox.Text, out var sellingPrice) || sellingPrice < 0)
        {
            ShowError("سعر البيع غير صحيح");
            return;
        }
        if (!int.TryParse(ReorderLevelBox.Text, out var reorderLevel) || reorderLevel < 0)
        {
            ShowError("حد إعادة الطلب غير صحيح");
            return;
        }

        var type = SparePartRadio.IsChecked == true ? ItemType.SparePart : ItemType.Accessory;
        var unit = (UnitBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "قطعة";

        SaveButton.IsEnabled = false;
        try
        {
            CreatedItem = await _itemService.CreateAsync(new CreateItemDto(
                code, name, type, unit, costPrice, sellingPrice, reorderLevel));
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
