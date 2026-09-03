using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Login succeeded and no password change is required - go to the main window.</summary>
    public event Action? LoginSucceeded;

    /// <summary>Login succeeded but the account must set a new password first.</summary>
    public event Action? MustChangePassword;

    public event Action? LoginFailed;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        IsLoading = true;
        ErrorMessage = "";

        try
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "يرجى إدخال اسم المستخدم";
                LoginFailed?.Invoke();
                return;
            }

            var result = await _authService.LoginAsync(new LoginDto(Username, Password));

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "فشل تسجيل الدخول";
                LoginFailed?.Invoke();
            }
            else if (result.MustChangePassword)
                MustChangePassword?.Invoke();
            else
                LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "تسجيل الدخول");
            LoginFailed?.Invoke();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
