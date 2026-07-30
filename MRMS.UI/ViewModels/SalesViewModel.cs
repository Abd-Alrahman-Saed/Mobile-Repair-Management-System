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

    [ObservableProperty]
    private ObservableCollection<SaleDto> _sales = [];

    [ObservableProperty]
    private bool _isLoading;

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
        var items = await _saleService.GetAllAsync();
        _allSales = items.ToList();
        Sales = new ObservableCollection<SaleDto>(_allSales);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task DeleteSaleAsync(int id)
    {
        var confirm = System.Windows.MessageBox.Show(
            "هل أنت متأكد من حذف عملية البيع هذه؟", "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes)
            return;

        await _saleService.DeleteAsync(id);
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
