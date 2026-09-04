using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class SaleFormPage : Page
{
    private readonly SaleFormViewModel _vm;

    public SaleFormPage(SaleFormViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        _vm.ItemScanned += () => QuantityBox.Focus();
        _vm.RequestApproval = RequestOwnerApproval;

        Loaded += async (_, _) =>
        {
            await vm.LoadCommand.ExecuteAsync(null);
            ScanBox.Focus();
        };
    }

    private void ScanBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.ScanItemCommand.Execute(null);
    }

    private int? RequestOwnerApproval(string message)
    {
        var dialog = App.ServiceProvider!.GetRequiredService<OwnerApprovalWindow>();
        dialog.Owner = Window.GetWindow(this);
        dialog.SetMessage(message);
        return dialog.ShowDialog() == true ? dialog.ApproverUserId : null;
    }
}
