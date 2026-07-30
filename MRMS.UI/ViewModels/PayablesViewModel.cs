using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;

namespace MRMS.UI.ViewModels;

public partial class PayablesViewModel : ObservableObject
{
    private readonly IReportService _reportService;

    [ObservableProperty]
    private ObservableCollection<PayableItemDto> _suppliers = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private decimal _totalOutstanding;

    public PayablesViewModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _reportService.GetPayablesAsync();
        Suppliers = new ObservableCollection<PayableItemDto>(items);
        TotalOutstanding = items.Sum(s => s.OutstandingBalance);
        IsLoading = false;
    }
}
