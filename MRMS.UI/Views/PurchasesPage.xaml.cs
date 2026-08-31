using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class PurchasesPage : Page
{
    private readonly PurchasesViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public PurchasesPage(PurchasesViewModel vm, AppNavigationService navigationService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;

        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }

    private void AddPurchase_Click(object sender, RoutedEventArgs e)
    {
        var page = App.ServiceProvider!.GetRequiredService<PurchaseFormPage>();
        _navigationService.NavigateTo(page);
    }

    private void Purchases_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Ignore double-clicks that landed on the delete button (or any button) inside the row.
        if (e.OriginalSource is DependencyObject source && FindAncestor<Button>(source) is not null)
            return;

        if (sender is not DataGrid grid || grid.SelectedItem is not PurchaseDto purchase)
            return;

        var purchaseService = App.ServiceProvider!.GetRequiredService<IPurchaseService>();
        var window = new PurchaseDetailWindow(purchaseService, purchase.Id)
        {
            Owner = Window.GetWindow(this)
        };
        window.ShowDialog();
    }

    private static T? FindAncestor<T>(DependencyObject source) where T : DependencyObject
    {
        var current = source;
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
