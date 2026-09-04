using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class ItemsPage : Page
{
    private readonly ItemsViewModel _vm;

    public ItemsPage(ItemsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        if (!vm.CanViewCost)
            CostColumn.Visibility = System.Windows.Visibility.Collapsed;

        Loaded += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void AddItem_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var page = App.ServiceProvider!.GetRequiredService<ItemFormPage>();
        App.ServiceProvider!.GetRequiredService<Services.AppNavigationService>().NavigateTo(page);
    }
}
