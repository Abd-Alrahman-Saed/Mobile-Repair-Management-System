using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;

namespace MRMS.UI.ViewModels;

public partial class IncomeStatementViewModel : ObservableObject
{
    private readonly IIncomeStatementService _incomeStatementService;

    public IncomeStatementViewModel(IIncomeStatementService incomeStatementService)
    {
        _incomeStatementService = incomeStatementService;
        From = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        To = DateTime.Today;
    }

    [ObservableProperty]
    private DateTime _from;

    [ObservableProperty]
    private DateTime _to;

    [ObservableProperty]
    private IncomeStatementDto? _statement;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        Statement = await _incomeStatementService.GetIncomeStatementAsync(From, To);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task ThisMonthAsync()
    {
        From = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        To = DateTime.Today;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LastMonthAsync()
    {
        var firstOfLastMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        From = firstOfLastMonth;
        To = firstOfLastMonth.AddMonths(1).AddDays(-1);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ThisYearAsync()
    {
        From = new DateTime(DateTime.Today.Year, 1, 1);
        To = DateTime.Today;
        await LoadAsync();
    }
}
