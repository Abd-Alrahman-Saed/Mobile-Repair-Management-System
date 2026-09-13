using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class CustomerListViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;
    private readonly IReportService _reportService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private List<CustomerListItem> _allCustomers = [];

    [ObservableProperty]
    private ObservableCollection<CustomerListItem> _customers = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = "";

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Customers = new ObservableCollection<CustomerListItem>(_allCustomers);
        else
            Customers = new ObservableCollection<CustomerListItem>(
                _allCustomers.Where(c =>
                    c.FullName.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    c.PhoneNumber.Contains(value)));
    }

    public CustomerListViewModel(ICustomerService customerService, IReportService reportService,
        AppNavigationService navigationService, NotificationService notificationService)
    {
        _customerService = customerService;
        _reportService = reportService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _customerService.GetAllAsync();
        var receivables = (await _reportService.GetReceivablesAsync()).ToDictionary(r => r.CustomerId);

        _allCustomers = items.Select(c =>
        {
            receivables.TryGetValue(c.Id, out var r);
            return new CustomerListItem
            {
                Id = c.Id,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                CreatedAt = c.CreatedAt,
                OutstandingBalance = r?.OutstandingBalance ?? 0m
            };
        }).ToList();

        Customers = new ObservableCollection<CustomerListItem>(_allCustomers);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync(int id)
    {
        var hasDevices = await _customerService.HasDevicesOrRepairsAsync(id);
        if (hasDevices)
        {
            System.Windows.MessageBox.Show(
                "لا يمكن حذف العميل لأنه يمتلك أجهزة أو تذاكر صيانة",
                "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var confirm = System.Windows.MessageBox.Show(
            "هل أنت متأكد من حذف هذا العميل؟", "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes)
            return;

        await _customerService.DeleteAsync(id);
        _notificationService.Show("تم حذف العميل بنجاح", NotificationType.Success);
        await LoadAsync();
    }

    [RelayCommand]
    private void AddCustomer()
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.CustomerFormPage>();
        _navigationService.NavigateTo(page);
    }

    [RelayCommand]
    private void EditCustomer(int id)
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.CustomerFormPage>();
        _navigationService.NavigateTo(page, "EditCustomerId", id);
    }

    [RelayCommand]
    private void ViewProfile(int id)
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.CustomerProfilePage>();
        _navigationService.NavigateTo(page, "CustomerId", id);
    }
}

public class CustomerListItem
{
    public int Id { get; init; }
    public string FullName { get; init; } = "";
    public string PhoneNumber { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    /// <summary>Visible to everyone - no permission gate (unlike supplier balances).</summary>
    public decimal OutstandingBalance { get; init; }
}
