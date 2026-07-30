using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private List<SaleDto> _allSales = [];
    private const int PageSize = 20;

    [ObservableProperty]
    private ObservableCollection<SaleDto> _sales = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _currentPage = 1;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)_allSales.Count / PageSize));
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
    public string PageInfo => $"صفحة {CurrentPage} من {TotalPages}";

    public SalesViewModel(ISaleService saleService, AppNavigationService navigationService, NotificationService notificationService)
    {
        _saleService = saleService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        CurrentPage = 1;
        var items = await _saleService.GetAllAsync();
        _allSales = items.ToList();
        ApplyPaging();
        IsLoading = false;
    }

    [RelayCommand]
    private void NextPage()
    {
        if (HasNext) { CurrentPage++; ApplyPaging(); }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (HasPrevious) { CurrentPage--; ApplyPaging(); }
    }

    private void ApplyPaging()
    {
        var paged = _allSales.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        Sales = new ObservableCollection<SaleDto>(paged);
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
        OnPropertyChanged(nameof(PageInfo));
    }

    [RelayCommand]
    private async Task DeleteSaleAsync(int id)
    {
        var confirmed = await _notificationService.ConfirmAsync("هل أنت متأكد من حذف عملية البيع هذه؟");
        if (!confirmed) return;

        try { await _saleService.DeleteAsync(id); }
        catch (Exception ex) { _notificationService.Show(ex.Message, NotificationType.Error); return; }

        _notificationService.Show("تم حذف عملية البيع بنجاح", NotificationType.Success);
        await LoadAsync();
    }

    [RelayCommand]
    private void AddSale()
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.SaleFormPage>();
        _navigationService.NavigateTo(page);
    }
}
