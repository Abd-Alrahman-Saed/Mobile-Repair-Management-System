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

    [ObservableProperty]
    private ObservableCollection<PurchaseDto> _purchases = [];

    [ObservableProperty]
    private bool _isLoading;

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
        var items = await _purchaseService.GetAllAsync();
        _allPurchases = items.ToList();
        Purchases = new ObservableCollection<PurchaseDto>(_allPurchases);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task DeletePurchaseAsync(int id)
    {
        var confirm = System.Windows.MessageBox.Show(
            "هل أنت متأكد من حذف هذه الفاتورة؟", "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes)
            return;

        await _purchaseService.DeleteAsync(id);
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
