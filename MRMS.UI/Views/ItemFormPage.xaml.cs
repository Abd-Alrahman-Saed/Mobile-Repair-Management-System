using System.Windows.Controls;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class ItemFormPage : Page
{
    private readonly ItemFormViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public ItemFormPage(ItemFormViewModel vm, AppNavigationService navigationService, IItemService itemService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;

        Loaded += async (_, _) =>
        {
            var editId = _navigationService.GetParameter<int>("EditItemId");
            if (editId > 0)
            {
                var item = await itemService.GetByIdAsync(editId);
                if (item is not null)
                    _vm.SetEditData(item);
            }
            else
            {
                CodeBox.Focus();
            }
        };
    }

    private void CodeBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            NameBox.Focus();
            e.Handled = true;
        }
    }

    private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _navigationService.GoBack();
    }
}
