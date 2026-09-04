using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class AuditLogViewModel : ObservableObject
{
    private readonly IAuditService _auditService;

    [ObservableProperty] private ObservableCollection<AuditLogDto> _entries = [];
    [ObservableProperty] private ObservableCollection<string> _actions = [];
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private string _userFilter = "";
    [ObservableProperty] private string? _selectedAction;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = "";

    public AuditLogViewModel(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            if (Actions.Count == 0)
            {
                Actions = new ObservableCollection<string>(await _auditService.GetActionsAsync());
                Actions.Insert(0, "الكل");
            }

            var action = string.IsNullOrEmpty(SelectedAction) || SelectedAction == "الكل" ? null : SelectedAction;
            var user = string.IsNullOrWhiteSpace(UserFilter) ? null : UserFilter.Trim();
            var rows = await _auditService.QueryAsync(FromDate, ToDate, user, action);
            Entries = new ObservableCollection<AuditLogDto>(rows);
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "تحميل سجل التدقيق");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        FromDate = null;
        ToDate = null;
        UserFilter = "";
        SelectedAction = "الكل";
        await LoadAsync();
    }
}
