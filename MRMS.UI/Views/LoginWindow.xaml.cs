using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        _vm.LoginSucceeded += OnLoginSucceeded;
        _vm.LoginFailed += OnLoginFailed;
    }

    private async void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            await ExecuteLoginAsync();
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteLoginAsync();
    }

    private async Task ExecuteLoginAsync()
    {
        _vm.Password = PasswordBox.Password;
        await _vm.LoginCommand.ExecuteAsync(null);
    }

    private void OnLoginSucceeded()
    {
        var mainWindow = App.ServiceProvider!.GetRequiredService<MainWindow>();
        mainWindow.Show();
        Close();
    }

    private void OnLoginFailed()
    {
        PasswordBox.Clear();
        PasswordBox.Focus();
    }
}
