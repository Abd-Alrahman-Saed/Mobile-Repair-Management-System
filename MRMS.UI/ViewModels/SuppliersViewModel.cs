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
    private readonly IReportService _reportService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private List<SupplierListItem> _allSuppliers = [];

    [ObservableProperty]
    private ObservableCollection<SupplierListItem> _suppliers = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = "";

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Suppliers = new ObservableCollection<SupplierListItem>(_allSuppliers);
        else
            Suppliers = new ObservableCollection<SupplierListItem>(
                _allSuppliers.Where(s =>
                    s.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    s.Code.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    s.Phone.Contains(value)));
    }

    public SuppliersViewModel(
        ISupplierService supplierService,
        IReportService reportService,
        AppNavigationService navigationService,
        NotificationService notificationService)
    {
        _supplierService = supplierService;
        _reportService = reportService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var suppliers = await _supplierService.GetAllAsync();
        var payables = (await _reportService.GetPayablesAsync()).ToDictionary(p => p.SupplierId);

        _allSuppliers = suppliers
            .Select(s =>
            {
                payables.TryGetValue(s.Id, out var p);
                return new SupplierListItem
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    Phone = s.Phone,
                    Notes = s.Notes,
                    OpeningBalance = s.OpeningBalance,
                    TotalPurchases = p?.TotalPurchases ?? 0,
                    TotalPayments = p?.TotalPayments ?? 0,
                    OutstandingBalance = p?.OutstandingBalance ?? s.OpeningBalance
                };
            })
            .ToList();

        Suppliers = new ObservableCollection<SupplierListItem>(_allSuppliers);
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

public class SupplierListItem
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Notes { get; init; } = "";
    public decimal OpeningBalance { get; init; }
    public decimal TotalPurchases { get; init; }
    public decimal TotalPayments { get; init; }
    public decimal OutstandingBalance { get; init; }
}
