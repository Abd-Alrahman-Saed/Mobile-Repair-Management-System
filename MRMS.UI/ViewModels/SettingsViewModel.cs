using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISystemSettingService _settingService;
    private readonly IAuthService _authService;
    private readonly IBackupService _backupService;
    private readonly NotificationService _notificationService;
    private List<SystemSettingDto> _allSettings = [];

    [ObservableProperty]
    private ObservableCollection<string> _paymentMethodsList = [];

    [ObservableProperty]
    private string _newPaymentMethod = "";

    [ObservableProperty]
    private ObservableCollection<string> _expenseCategoriesList = [];

    [ObservableProperty]
    private string _newExpenseCategory = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    // ===== Change password =====

    [ObservableProperty]
    private string _changePasswordErrorMessage = "";

    [ObservableProperty]
    private bool _isChangingPassword;

    // ===== Backup & Restore =====

    [ObservableProperty]
    private string _backupFolder = "";

    [ObservableProperty]
    private string _effectiveBackupFolder = "";

    [ObservableProperty]
    private int _retentionCount = 30;

    [ObservableProperty]
    private string _lastBackupDateText = "لم يتم أخذ أي نسخة بعد";

    [ObservableProperty]
    private bool _isBackingUp;

    [ObservableProperty]
    private bool _isRestoring;

    [ObservableProperty]
    private string _backupErrorMessage = "";

    [ObservableProperty]
    private string _restoreErrorMessage = "";

    [ObservableProperty]
    private ObservableCollection<BackupFileInfoDto> _backups = [];

    [ObservableProperty]
    private ObservableCollection<BackupHistoryEntryDto> _backupHistory = [];

    /// <summary>Raised after a restore attempt finishes: (success, errorMessage). On success the app must restart.</summary>
    public event Action<bool, string?>? RestoreCompleted;

    public SettingsViewModel(
        ISystemSettingService settingService,
        IAuthService authService,
        IBackupService backupService,
        NotificationService notificationService)
    {
        _settingService = settingService;
        _authService = authService;
        _backupService = backupService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var settings = await _settingService.GetAllAsync();
        _allSettings = settings.ToList();

        var paymentSetting = _allSettings.FirstOrDefault(s => s.Key == "PaymentMethods");
        PaymentMethodsList = new ObservableCollection<string>(ParseList(paymentSetting?.Value));

        var expenseSetting = _allSettings.FirstOrDefault(s => s.Key == "ExpenseCategories");
        ExpenseCategoriesList = new ObservableCollection<string>(ParseList(expenseSetting?.Value));

        LoadBackupSettings();
        await RefreshBackupListsAsync();

        IsLoading = false;
    }

    private static List<string> ParseList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    [RelayCommand]
    private void AddPaymentMethod()
    {
        var value = NewPaymentMethod.Trim();
        if (string.IsNullOrWhiteSpace(value)) return;
        if (PaymentMethodsList.Contains(value, StringComparer.OrdinalIgnoreCase)) { NewPaymentMethod = ""; return; }

        PaymentMethodsList.Add(value);
        NewPaymentMethod = "";
    }

    [RelayCommand]
    private void RemovePaymentMethod(string value)
    {
        PaymentMethodsList.Remove(value);
    }

    [RelayCommand]
    private void AddExpenseCategory()
    {
        var value = NewExpenseCategory.Trim();
        if (string.IsNullOrWhiteSpace(value)) return;
        if (ExpenseCategoriesList.Contains(value, StringComparer.OrdinalIgnoreCase)) { NewExpenseCategory = ""; return; }

        ExpenseCategoriesList.Add(value);
        NewExpenseCategory = "";
    }

    [RelayCommand]
    private void RemoveExpenseCategory(string value)
    {
        ExpenseCategoriesList.Remove(value);
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
                await _settingService.UpdateAsync(new UpdateSystemSettingDto(paymentSetting.Id, string.Join(",", PaymentMethodsList)));

            var expenseSetting = _allSettings.FirstOrDefault(s => s.Key == "ExpenseCategories");
            if (expenseSetting is not null)
                await _settingService.UpdateAsync(new UpdateSystemSettingDto(expenseSetting.Id, string.Join(",", ExpenseCategoriesList)));

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

    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
    {
        ChangePasswordErrorMessage = "";

        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            ChangePasswordErrorMessage = "يرجى إدخال كلمة المرور الحالية";
            return false;
        }
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            ChangePasswordErrorMessage = "يرجى إدخال كلمة المرور الجديدة";
            return false;
        }
        if (newPassword != confirmPassword)
        {
            ChangePasswordErrorMessage = "كلمة المرور الجديدة وتأكيدها غير متطابقين";
            return false;
        }

        IsChangingPassword = true;
        try
        {
            var result = await _authService.ChangePasswordAsync(
                new ChangePasswordDto(currentPassword, newPassword));

            if (!result.Success)
            {
                ChangePasswordErrorMessage = result.ErrorMessage ?? "تعذر تغيير كلمة المرور";
                return false;
            }

            _notificationService.Show("تم تغيير كلمة المرور بنجاح", NotificationType.Success);
            return true;
        }
        finally
        {
            IsChangingPassword = false;
        }
    }

    // ===== Backup & Restore =====

    private void LoadBackupSettings()
    {
        var settings = _backupService.GetSettings();
        BackupFolder = settings.BackupFolder;
        EffectiveBackupFolder = settings.EffectiveBackupFolder;
        RetentionCount = settings.RetentionCount;
        LastBackupDateText = settings.LastBackupDate is { } date
            ? date.ToString("yyyy-MM-dd")
            : "لم يتم أخذ أي نسخة بعد";
    }

    private async Task RefreshBackupListsAsync()
    {
        var backups = await _backupService.ListBackupsAsync();
        Backups = new ObservableCollection<BackupFileInfoDto>(backups);

        var history = await _backupService.GetHistoryAsync();
        BackupHistory = new ObservableCollection<BackupHistoryEntryDto>(history);
    }

    public async Task SetBackupFolderAsync(string folder)
    {
        await _backupService.UpdateSettingsAsync(folder, null);
        LoadBackupSettings();
        await RefreshBackupListsAsync();
        _notificationService.Show("تم تحديد مجلد النسخ الاحتياطية", NotificationType.Success);
    }

    [RelayCommand]
    private async Task SaveRetentionAsync()
    {
        if (RetentionCount < 1) RetentionCount = 1;
        await _backupService.UpdateSettingsAsync(null, RetentionCount);
        _notificationService.Show("تم حفظ عدد النسخ المحتفظ بها", NotificationType.Success);
    }

    [RelayCommand]
    private async Task BackupNowAsync()
    {
        BackupErrorMessage = "";
        IsBackingUp = true;
        try
        {
            var result = await _backupService.BackupNowAsync(isAutomatic: false);
            if (result.Success)
            {
                var message = result.UsedFallbackFolder
                    ? "تم أخذ النسخة الاحتياطية، لكن تم استخدام المجلد الاحتياطي لأن المجلد المحدد غير متاح"
                    : "تم أخذ النسخة الاحتياطية بنجاح";
                _notificationService.Show(message, result.UsedFallbackFolder ? NotificationType.Warning : NotificationType.Success);
                LoadBackupSettings();
                await RefreshBackupListsAsync();
            }
            else
            {
                BackupErrorMessage = result.ErrorMessage ?? "فشل أخذ النسخة الاحتياطية";
                _notificationService.Show(BackupErrorMessage, NotificationType.Error);
            }
        }
        finally
        {
            IsBackingUp = false;
        }
    }

    [RelayCommand]
    private async Task RestoreAsync(BackupFileInfoDto backup)
    {
        await RestoreFromPathAsync(backup.FullPath);
    }

    public async Task RestoreFromPathAsync(string path)
    {
        var confirmed = await _notificationService.ConfirmAsync(
            $"سيتم استبدال كل البيانات الحالية بالنسخة الاحتياطية المختارة ({Path.GetFileName(path)}). " +
            "سيتم أخذ نسخة أمان من البيانات الحالية أولاً تلقائيًا، لكن هذا الإجراء يستبدل بياناتك الحالية بالكامل. هل أنت متأكد؟");
        if (!confirmed) return;

        RestoreErrorMessage = "";
        IsRestoring = true;
        try
        {
            var result = await _backupService.RestoreAsync(path);
            if (result.Success)
            {
                RestoreCompleted?.Invoke(true, null);
            }
            else
            {
                RestoreErrorMessage = result.ErrorMessage ?? "فشل الاسترجاع";
                _notificationService.Show(RestoreErrorMessage, NotificationType.Error);
                RestoreCompleted?.Invoke(false, RestoreErrorMessage);
            }
        }
        finally
        {
            IsRestoring = false;
        }
    }
}
