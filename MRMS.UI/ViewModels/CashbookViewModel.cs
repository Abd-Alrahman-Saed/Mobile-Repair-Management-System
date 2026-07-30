using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;

namespace MRMS.UI.ViewModels;

public partial class CashbookViewModel : ObservableObject
{
    private readonly IReportService _reportService;

    [ObservableProperty]
    private ObservableCollection<CashbookEntryDto> _entries = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime _fromDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    [ObservableProperty]
    private decimal _openingBalance;

    [ObservableProperty]
    private decimal _totalInflows;

    [ObservableProperty]
    private decimal _totalOutflows;

    [ObservableProperty]
    private decimal _closingBalance;

    public CashbookViewModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var report = await _reportService.GetCashbookAsync(FromDate, ToDate);
        Entries = new ObservableCollection<CashbookEntryDto>(report.Entries);
        OpeningBalance = report.OpeningBalance;
        TotalInflows = report.TotalInflows;
        TotalOutflows = report.TotalOutflows;
        ClosingBalance = report.ClosingBalance;
        IsLoading = false;
    }
}
