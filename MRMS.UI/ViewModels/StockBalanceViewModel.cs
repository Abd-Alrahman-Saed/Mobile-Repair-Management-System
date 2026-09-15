using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;

namespace MRMS.UI.ViewModels;

public partial class StockBalanceViewModel : ObservableObject
{
    private readonly IReportService _reportService;

    [ObservableProperty]
    private ObservableCollection<StockBalanceItemDto> _items = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private decimal _totalStockValue;

    public StockBalanceViewModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _reportService.GetStockBalanceAsync();
        Items = new ObservableCollection<StockBalanceItemDto>(items);
        TotalStockValue = items.Sum(i => i.StockValue ?? 0m);
        IsLoading = false;
    }
}
