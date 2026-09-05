using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class CashRegisterViewModel : ObservableObject
{
    private readonly ICashSessionService _cashSessions;
    private readonly NotificationService _notificationService;

    public CashRegisterViewModel(ICashSessionService cashSessions, NotificationService notificationService)
    {
        _cashSessions = cashSessions;
        _notificationService = notificationService;
    }

    [ObservableProperty]
    private CashSessionDto? _currentSession;

    [ObservableProperty]
    private bool _hasOpenSession;

    [ObservableProperty]
    private decimal _openingBalanceInput;

    [ObservableProperty]
    private decimal _countedAmountInput;

    [ObservableProperty]
    private string _closeNotes = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = "";

    public ObservableCollection<CashSessionDto> History { get; } = [];

    public decimal ExpectedAmount => CurrentSession?.ExpectedAmount ?? 0m;

    /// <summary>Counted minus expected: what the close will record. Positive = surplus, negative = shortage.</summary>
    public decimal Difference => CountedAmountInput - ExpectedAmount;

    public string DifferenceBrushKey =>
        Difference > 0 ? "SuccessBrush" : Difference < 0 ? "DangerBrush" : "TextSecondaryBrush";

    partial void OnCurrentSessionChanged(CashSessionDto? value)
    {
        OnPropertyChanged(nameof(ExpectedAmount));
        OnPropertyChanged(nameof(Difference));
        OnPropertyChanged(nameof(DifferenceBrushKey));
    }

    partial void OnCountedAmountInputChanged(decimal value)
    {
        OnPropertyChanged(nameof(Difference));
        OnPropertyChanged(nameof(DifferenceBrushKey));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            CurrentSession = await _cashSessions.GetCurrentAsync();
            HasOpenSession = CurrentSession is not null;
            CountedAmountInput = 0;
            CloseNotes = "";

            History.Clear();
            foreach (var s in await _cashSessions.GetHistoryAsync())
                History.Add(s);
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "درج النقدية");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenSessionAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            await _cashSessions.OpenAsync(OpeningBalanceInput);
            OpeningBalanceInput = 0;
            _notificationService.Show("تم فتح الوردية", NotificationType.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "فتح الوردية");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CloseSessionAsync()
    {
        if (!await _notificationService.ConfirmAsync(
                $"إقفال اليومية بمبلغ معدود {CountedAmountInput:N2} وفرق {Difference:N2}؟"))
            return;

        IsLoading = true;
        ErrorMessage = "";
        try
        {
            await _cashSessions.CloseAsync(CountedAmountInput, CloseNotes);
            _notificationService.Show("تم إقفال اليومية", NotificationType.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "إقفال اليومية");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
