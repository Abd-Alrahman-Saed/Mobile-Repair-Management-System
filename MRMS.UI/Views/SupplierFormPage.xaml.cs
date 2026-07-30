using System.Windows.Controls;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class SupplierFormPage : Page
{
    private readonly SupplierFormViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public SupplierFormPage(SupplierFormViewModel vm, AppNavigationService navigationService, ISupplierService supplierService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;

        Loaded += async (_, _) =>
        {
            var editId = _navigationService.GetParameter<int>("EditSupplierId");
            if (editId > 0)
            {
                var supplier = await supplierService.GetByIdAsync(editId);
                if (supplier is not null)
                    _vm.SetEditData(supplier);
            }
        };
    }

    private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        _navigationService.GoBack();
    }
}
