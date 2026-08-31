using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class SettingsPage : Page
{
    private readonly SettingsViewModel _vm;

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);

        _vm.RestoreCompleted += OnRestoreCompleted;
        Unloaded += (_, _) => _vm.RestoreCompleted -= OnRestoreCompleted;
    }

    private void NewPaymentMethod_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.AddPaymentMethodCommand.Execute(null);
    }

    private void NewExpenseCategory_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.AddExpenseCategoryCommand.Execute(null);
    }

    private async void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var success = await _vm.ChangePasswordAsync(
            CurrentPasswordBox.Password, NewPasswordBox.Password, ConfirmPasswordBox.Password);

        if (success)
        {
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }
    }

    private async void ChooseBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "اختر مجلد حفظ النسخ الاحتياطية"
        };
        if (dialog.ShowDialog() == true)
        {
            await _vm.SetBackupFolderAsync(dialog.FolderName);
        }
    }

    private async void BrowseRestoreFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختر ملف نسخة احتياطية للاسترجاع",
            Filter = "SQL Server Backup (*.bak)|*.bak|كل الملفات|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            await _vm.RestoreFromPathAsync(dialog.FileName);
        }
    }

    private void OnRestoreCompleted(bool success, string? errorMessage)
    {
        if (!success) return;

        Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                "تم استرجاع البيانات بنجاح. يجب إعادة تشغيل البرنامج الآن حتى تظهر البيانات الجديدة بشكل صحيح.",
                "تم الاسترجاع بنجاح",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            RestartApplication();
        });
    }

    private static void RestartApplication()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(exePath))
            Process.Start(exePath);

        System.Windows.Application.Current.Shutdown();
    }
}
