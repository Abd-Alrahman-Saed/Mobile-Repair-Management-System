using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class SupplierPaymentsViewModel : ObservableObject
{
    private readonly ISupplierPaymentService _paymentService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private List<SupplierPaymentDto> _allPayments = [];

    [ObservableProperty]
    private ObservableCollection<SupplierPaymentDto> _payments = [];

    [ObservableProperty]
    private bool _isLoading;

    public SupplierPaymentsViewModel(ISupplierPaymentService paymentService, AppNavigationService navigationService, NotificationService notificationService)
    {
        _paymentService = paymentService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _paymentService.GetAllAsync();
        _allPayments = items.ToList();
        Payments = new ObservableCollection<SupplierPaymentDto>(_allPayments);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task VoidPaymentAsync(int id)
    {
        var reason = Views.VoidReasonWindow.Ask(System.Windows.Application.Current.MainWindow,
            "سبب إلغاء الدفعة");
        if (reason is null) return;

        try { await _paymentService.VoidAsync(id, reason); }
        catch (Exception ex) { _notificationService.Show(UiError.Describe(ex, "إلغاء الدفعة"), NotificationType.Error); return; }

        _notificationService.Show("تم إلغاء الدفعة", NotificationType.Success);
        await LoadAsync();
    }

    [RelayCommand]
    private void AddPayment()
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.SupplierPaymentFormPage>();
        _navigationService.NavigateTo(page);
    }
}
