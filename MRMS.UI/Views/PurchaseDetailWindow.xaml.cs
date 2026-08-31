using System.Windows;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;

namespace MRMS.UI.Views;

public partial class PurchaseDetailWindow : Window
{
    private readonly IPurchaseService _purchaseService;
    private readonly int _purchaseId;

    public PurchaseDetailWindow(IPurchaseService purchaseService, int purchaseId)
    {
        InitializeComponent();
        _purchaseService = purchaseService;
        _purchaseId = purchaseId;

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var purchase = await _purchaseService.GetByIdAsync(_purchaseId);
        if (purchase is null)
        {
            MessageBox.Show("لم يتم العثور على الفاتورة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        DateText.Text = purchase.Date.ToString("yyyy-MM-dd");
        SupplierText.Text = purchase.SupplierName;
        PaymentMethodText.Text = purchase.PaymentMethod switch
        {
            PaymentMethod.Cash => "نقداً",
            PaymentMethod.Credit => "آجل",
            _ => purchase.PaymentMethod.ToString()
        };
        PaidAmountText.Text = purchase.PaidAmount.ToString("N0");
        NotesText.Text = string.IsNullOrWhiteSpace(purchase.Notes) ? "-" : purchase.Notes;
        ItemsGrid.ItemsSource = purchase.Items;
        TotalCostRun.Text = purchase.TotalCost.ToString("N0");
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
