using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.Interfaces;
using MRMS.Core.Authorization;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class OwnerApprovalViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _isLoading;

    public int? ApproverUserId { get; private set; }

    /// <summary>Raised when an authorised approver was verified - the host closes with a positive result.</summary>
    public event Action? Approved;

    public OwnerApprovalViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        ErrorMessage = "";
        IsLoading = true;
        try
        {
            var user = await _authService.VerifyCredentialsAsync(Username, Password);
            if (user is null || !RolePermissions.Has(user.Role, Permission.ApplyDiscountBeyondLimit))
            {
                ErrorMessage = "بيانات غير صحيحة أو المستخدم لا يملك صلاحية الاعتماد";
                return;
            }
            ApproverUserId = user.Id;
            Approved?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "اعتماد العملية");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
