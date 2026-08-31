using System.Windows;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;

namespace MRMS.UI.Views;

public partial class QuickPriceEditWindow : Window
{
    private readonly IItemService _itemService;
    private ItemDto? _item;

    public decimal SavedCostPrice { get; private set; }
    public decimal SavedSellingPrice { get; private set; }

    public QuickPriceEditWindow(IItemService itemService)
    {
        InitializeComponent();
        _itemService = itemService;
    }

    public void LoadItem(ItemDto item)
    {
        _item = item;
        ItemInfoText.Text = $"{item.Name} ({item.Code})";
        CostPriceBox.Text = item.CostPrice.ToString("0.##");
        SellingPriceBox.Text = item.SellingPrice.ToString("0.##");
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_item is null)
        {
            DialogResult = false;
            Close();
            return;
        }

        ErrorBorder.Visibility = Visibility.Collapsed;

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

        SaveButton.IsEnabled = false;
        try
        {
            await _itemService.UpdatePricesAsync(_item.Id, costPrice, sellingPrice);
            SavedCostPrice = costPrice;
            SavedSellingPrice = sellingPrice;
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
