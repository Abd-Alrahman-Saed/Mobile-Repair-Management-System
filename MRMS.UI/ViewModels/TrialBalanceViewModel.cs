using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;

namespace MRMS.UI.ViewModels;

public partial class TrialBalanceViewModel : ObservableObject
{
    private readonly IJournalService _journalService;

    public TrialBalanceViewModel(IJournalService journalService)
    {
        _journalService = journalService;
    }

    [ObservableProperty]
    private TrialBalanceDto? _trialBalance;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime? _asOfDate;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        TrialBalance = await _journalService.GetTrialBalanceAsync(AsOfDate);
        IsLoading = false;
    }
}
