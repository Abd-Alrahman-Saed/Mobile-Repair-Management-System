using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISystemSettingService _settingService;
    private readonly NotificationService _notificationService;
    private List<SystemSettingDto> _allSettings = [];

    [ObservableProperty]
    private string _paymentMethods = "";

    [ObservableProperty]
    private string _expenseCategories = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public SettingsViewModel(ISystemSettingService settingService, NotificationService notificationService)
    {
        _settingService = settingService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var settings = await _settingService.GetAllAsync();
        _allSettings = settings.ToList();

        var paymentSetting = _allSettings.FirstOrDefault(s => s.Key == "PaymentMethods");
        PaymentMethods = paymentSetting?.Value ?? "";

        var expenseSetting = _allSettings.FirstOrDefault(s => s.Key == "ExpenseCategories");
        ExpenseCategories = expenseSetting?.Value ?? "";

        IsLoading = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";
        IsLoading = true;
        try
        {
            var paymentSetting = _allSettings.FirstOrDefault(s => s.Key == "PaymentMethods");
            if (paymentSetting is not null)
                await _settingService.UpdateAsync(new UpdateSystemSettingDto(paymentSetting.Id, PaymentMethods));

            var expenseSetting = _allSettings.FirstOrDefault(s => s.Key == "ExpenseCategories");
            if (expenseSetting is not null)
                await _settingService.UpdateAsync(new UpdateSystemSettingDto(expenseSetting.Id, ExpenseCategories));

            _notificationService.Show("تم حفظ الإعدادات بنجاح", NotificationType.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
