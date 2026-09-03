using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class PurchasesViewModel : ObservableObject
{
    private readonly IPurchaseService _purchaseService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private List<PurchaseDto> _allPurchases = [];
    private const int PageSize = 20;

    [ObservableProperty]
    private ObservableCollection<PurchaseDto> _purchases = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _currentPage = 1;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)_allPurchases.Count / PageSize));
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
    public string PageInfo => $"صفحة {CurrentPage} من {TotalPages}";

    public PurchasesViewModel(IPurchaseService purchaseService, AppNavigationService navigationService, NotificationService notificationService)
    {
        _purchaseService = purchaseService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        CurrentPage = 1;
        var items = await _purchaseService.GetAllAsync();
        _allPurchases = items.ToList();
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
        var paged = _allPurchases.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        Purchases = new ObservableCollection<PurchaseDto>(paged);
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
        OnPropertyChanged(nameof(PageInfo));
    }

    [RelayCommand]
    private async Task DeletePurchaseAsync(int id)
    {
        var confirmed = await _notificationService.ConfirmAsync("هل أنت متأكد من حذف هذه الفاتورة؟");
        if (!confirmed) return;

        try { await _purchaseService.DeleteAsync(id); }
        catch (Exception ex) { _notificationService.Show(UiError.Describe(ex, "حذف فاتورة المشتريات"), NotificationType.Error); return; }

        _notificationService.Show("تم حذف فاتورة المشتريات بنجاح", NotificationType.Success);
        await LoadAsync();
    }

    [RelayCommand]
    private void AddPurchase()
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.PurchaseFormPage>();
        _navigationService.NavigateTo(page);
    }
}
