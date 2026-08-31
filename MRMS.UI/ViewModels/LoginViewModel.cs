using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using Serilog;

namespace MRMS.UI.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public event Action? LoginSucceeded;
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
            var result = await _authService.LoginAsync(new LoginDto(Password));

            if (result.Success)
                LoginSucceeded?.Invoke();
            else
            {
                ErrorMessage = result.ErrorMessage ?? "فشل تسجيل الدخول";
                LoginFailed?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Login failed");
            ErrorMessage = $"خطأ: {ex.Message}";
            LoginFailed?.Invoke();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
