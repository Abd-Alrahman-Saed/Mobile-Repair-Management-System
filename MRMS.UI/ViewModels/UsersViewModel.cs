using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;

    [ObservableProperty] private ObservableCollection<UserDto> _users = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = "";

    public UsersViewModel(IUserService userService, AppNavigationService navigationService,
        NotificationService notificationService)
    {
        _userService = userService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            Users = new ObservableCollection<UserDto>(await _userService.GetAllAsync());
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "تحميل المستخدمين");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddUser()
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<Views.UserFormPage>());

    [RelayCommand]
    private void EditUser(int id)
        => _navigationService.NavigateTo(App.ServiceProvider!.GetRequiredService<Views.UserFormPage>(), "EditUserId", id);

    [RelayCommand]
    private async Task ToggleActiveAsync(UserDto user)
    {
        try
        {
            await _userService.UpdateAsync(new UpdateUserDto(user.Id, user.FullName, user.Role, !user.IsActive));
            _notificationService.Show(user.IsActive ? "تم إيقاف المستخدم" : "تم تفعيل المستخدم", NotificationType.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _notificationService.Show(UiError.Describe(ex, "تغيير حالة المستخدم"), NotificationType.Error);
        }
    }
}
