using System.Windows.Controls;
using System.Windows.Input;
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
        Loaded += async (_, _) =>
        {
            await _vm.LoadCustomersCommand.ExecuteAsync(null);
            ImeiBox.Focus();
        };
    }

    private void Imei_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.FindDeviceCommand.Execute(null);
    }
}
