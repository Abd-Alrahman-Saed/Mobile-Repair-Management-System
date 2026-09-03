using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty] private string _currentPassword = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _confirmPassword = "";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _isLoading;

    /// <summary>The password was changed successfully - the host window should close with a positive result.</summary>
    public event Action? Changed;

    public ChangePasswordViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";

        if (NewPassword.Length < 4)
        {
            ErrorMessage = "كلمة المرور الجديدة يجب أن تكون 4 أحرف على الأقل";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "كلمتا المرور غير متطابقتين";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _authService.ChangePasswordAsync(
                new ChangePasswordDto(CurrentPassword, NewPassword));

            if (result.Success)
                Changed?.Invoke();
            else
                ErrorMessage = result.ErrorMessage ?? "تعذّر تغيير كلمة المرور";
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "تغيير كلمة المرور");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
