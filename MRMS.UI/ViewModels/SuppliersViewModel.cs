using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class SuppliersViewModel : ObservableObject
{
    private readonly ISupplierService _supplierService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private List<SupplierDto> _allSuppliers = [];

    [ObservableProperty]
    private ObservableCollection<SupplierDto> _suppliers = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = "";

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Suppliers = new ObservableCollection<SupplierDto>(_allSuppliers);
        else
            Suppliers = new ObservableCollection<SupplierDto>(
                _allSuppliers.Where(s =>
                    s.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    s.Code.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    s.Phone.Contains(value)));
    }

    public SuppliersViewModel(ISupplierService supplierService, AppNavigationService navigationService, NotificationService notificationService)
    {
        _supplierService = supplierService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _supplierService.GetAllAsync();
        _allSuppliers = items.ToList();
        Suppliers = new ObservableCollection<SupplierDto>(_allSuppliers);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task DeleteSupplierAsync(int id)
    {
        var confirm = System.Windows.MessageBox.Show(
            "هل أنت متأكد من حذف هذا المورد؟", "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes)
            return;

        await _supplierService.DeleteAsync(id);
        _notificationService.Show("تم حذف المورد بنجاح", NotificationType.Success);
        await LoadAsync();
    }

    [RelayCommand]
    private void AddSupplier()
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.SupplierFormPage>();
        _navigationService.NavigateTo(page);
    }

    [RelayCommand]
    private void EditSupplier(int id)
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.SupplierFormPage>();
        _navigationService.NavigateTo(page, "EditSupplierId", id);
    }
}
