using System.Windows.Controls;
using System.Windows.Navigation;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class CustomerFormPage : Page
{
    private readonly CustomerFormViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public CustomerFormPage(CustomerFormViewModel vm, AppNavigationService navigationService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;
        Loaded += async (_, _) =>
        {
            var editId = _navigationService.GetParameter<int?>("EditCustomerId");
            if (editId.HasValue)
                await _vm.LoadForEditAsync(editId.Value);
        };
        _vm.SaveCompleted += () => _navigationService.GoBack();
    }

    private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        => _navigationService.GoBack();
}
