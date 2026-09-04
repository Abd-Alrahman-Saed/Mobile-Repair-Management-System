using System.Windows;
using System.Windows.Controls;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class UserFormPage : Page
{
    private readonly UserFormViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public UserFormPage(UserFormViewModel vm, AppNavigationService navigationService, IUserService userService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;

        Loaded += async (_, _) =>
        {
            var editId = _navigationService.GetParameter<int>("EditUserId");
            if (editId > 0)
            {
                var user = await userService.GetByIdAsync(editId);
                if (user is not null)
                    _vm.SetEditData(user);
            }
        };
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!_vm.IsEditMode)
            _vm.Password = PasswordBox.Password;
        await _vm.SaveCommand.ExecuteAsync(null);
    }

    private async void ResetPassword_Click(object sender, RoutedEventArgs e)
    {
        _vm.NewPassword = NewPasswordBox.Password;
        await _vm.ResetPasswordCommand.ExecuteAsync(null);
        NewPasswordBox.Clear();
    }
}
