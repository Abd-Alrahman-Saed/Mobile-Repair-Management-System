using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class SupplierPaymentFormViewModel : ObservableObject
{
    private readonly ISupplierPaymentService _paymentService;
    private readonly ISupplierService _supplierService;
    private readonly IReportService _reportService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private Dictionary<int, PayableItemDto> _payablesBySupplier = [];

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private int _selectedSupplierId;

    [ObservableProperty]
    private ObservableCollection<SupplierDto> _suppliers = [];

    [ObservableProperty]
    private decimal _outstandingBalance;

    partial void OnSelectedSupplierIdChanged(int value)
    {
        if (value > 0 && _payablesBySupplier.TryGetValue(value, out var payable))
            OutstandingBalance = payable.OutstandingBalance;
        else
            OutstandingBalance = 0;
    }

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private string _reference = "";

    [ObservableProperty]
    private string _notes = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public SupplierPaymentFormViewModel(
        ISupplierPaymentService paymentService,
        ISupplierService supplierService,
        IReportService reportService,
        AppNavigationService navigationService,
        NotificationService notificationService)
    {
        _paymentService = paymentService;
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
        var payables = await _reportService.GetPayablesAsync();
        _payablesBySupplier = payables.ToDictionary(p => p.SupplierId);
        Suppliers = new ObservableCollection<SupplierDto>(suppliers);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";

        if (SelectedSupplierId <= 0)
        {
            ErrorMessage = "يرجى اختيار المورد";
            return;
        }
        if (Amount <= 0)
        {
            ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر";
            return;
        }

        IsLoading = true;
        try
        {
            var dto = new CreateSupplierPaymentDto(Date, SelectedSupplierId, Amount, Reference, Notes);
            await _paymentService.CreateAsync(dto);
            _notificationService.Show("تم تسجيل الدفعة بنجاح", NotificationType.Success);
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }
}
