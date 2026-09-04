using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class UserFormViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private int _editId;

    public UserRole[] Roles { get; } = Enum.GetValues<UserRole>();

    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private UserRole _selectedRole = UserRole.Receptionist;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _isLoading;

    public UserFormViewModel(IUserService userService, AppNavigationService navigationService,
        NotificationService notificationService)
    {
        _userService = userService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    public void SetEditData(UserDto user)
    {
        _editId = user.Id;
        IsEditMode = true;
        Username = user.Username;
        FullName = user.FullName;
        SelectedRole = user.Role;
        IsActive = user.IsActive;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";
        IsLoading = true;
        try
        {
            if (IsEditMode)
            {
                await _userService.UpdateAsync(new UpdateUserDto(_editId, FullName, SelectedRole, IsActive));
                _notificationService.Show("تم تحديث المستخدم بنجاح", NotificationType.Success);
            }
            else
            {
                await _userService.CreateAsync(new CreateUserDto(Username, FullName, SelectedRole, Password));
                _notificationService.Show("تم إنشاء المستخدم بنجاح", NotificationType.Success);
            }
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "حفظ المستخدم");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        ErrorMessage = "";
        try
        {
            await _userService.ResetPasswordAsync(new ResetPasswordDto(_editId, NewPassword));
            NewPassword = "";
            _notificationService.Show("تم تعيين كلمة مرور جديدة، سيُطلب من المستخدم تغييرها عند الدخول", NotificationType.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "إعادة تعيين كلمة المرور");
        }
    }

    [RelayCommand]
    private void Cancel() => _navigationService.GoBack();
}
