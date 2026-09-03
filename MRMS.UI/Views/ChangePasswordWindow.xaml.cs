using System.Windows;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly ChangePasswordViewModel _vm;

    public ChangePasswordWindow(ChangePasswordViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        _vm.Changed += () =>
        {
            DialogResult = true;
            Close();
        };
        Loaded += (_, _) => CurrentBox.Focus();
    }

    private async void ConfirmBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            await ExecuteSaveAsync();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteSaveAsync();
    }

    private async Task ExecuteSaveAsync()
    {
        _vm.CurrentPassword = CurrentBox.Password;
        _vm.NewPassword = NewBox.Password;
        _vm.ConfirmPassword = ConfirmBox.Password;
        await _vm.SaveCommand.ExecuteAsync(null);
    }
}
