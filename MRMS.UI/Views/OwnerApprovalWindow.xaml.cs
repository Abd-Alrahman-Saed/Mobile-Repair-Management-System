using System.Windows;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class OwnerApprovalWindow : Window
{
    private readonly OwnerApprovalViewModel _vm;

    public int? ApproverUserId => _vm.ApproverUserId;

    public OwnerApprovalWindow(OwnerApprovalViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        _vm.Approved += () =>
        {
            DialogResult = true;
            Close();
        };
        Loaded += (_, _) => UsernameBox.Focus();
    }

    public void SetMessage(string message) => _vm.Message = message;

    private async void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            await ExecuteAsync();
    }

    private async void Approve_Click(object sender, RoutedEventArgs e) => await ExecuteAsync();

    private async Task ExecuteAsync()
    {
        _vm.Password = PasswordBox.Password;
        await _vm.ApproveCommand.ExecuteAsync(null);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
