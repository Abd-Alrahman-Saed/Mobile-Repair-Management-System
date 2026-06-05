using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class DeviceTransferPage : Page
{
    private readonly DeviceTransferViewModel _vm;

    public DeviceTransferPage(DeviceTransferViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadCustomersCommand.ExecuteAsync(null);
    }
}
