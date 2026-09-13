using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

/// <summary>One row in the customer's account statement - either an invoice (increases the balance
/// owed by its RemainingAmount, since any PaidAmount at sale time is already netted out) or a later
/// collection (decreases it). Newest first, like the cashbook.</summary>
public record CustomerStatementEntryVm(DateTime Date, string Description, decimal Delta, decimal RunningBalance);

public partial class CustomerProfileViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;
    private readonly IDeviceService _deviceService;
    private readonly IRepairTicketService _repairTicketService;
    private readonly ISaleService _saleService;
    private readonly ICustomerPaymentService _customerPaymentService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private CustomerDto? _customer;

    [ObservableProperty]
    private ObservableCollection<CustomerStatementEntryVm> _statement = [];

    [ObservableProperty]
    private decimal _outstandingBalance;

    [ObservableProperty]
    private decimal _collectAmount;

    [ObservableProperty]
    private string _collectReference = "";

    [ObservableProperty]
    private ObservableCollection<DeviceDto> _devices = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDevice))]
    private DeviceDto? _selectedDevice;

    public bool HasSelectedDevice => SelectedDevice is not null;

    [ObservableProperty]
    private ObservableCollection<RepairTicketDto> _selectedDeviceTickets = [];

    [ObservableProperty]
    private string _newTicketProblemDescription = "";

    [ObservableProperty]
    private string _newTicketReceptionNotes = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public event Action<int>? TicketCreated;

    public CustomerProfileViewModel(
        ICustomerService customerService,
        IDeviceService deviceService,
        IRepairTicketService repairTicketService,
        ISaleService saleService,
        ICustomerPaymentService customerPaymentService,
        AppNavigationService navigationService,
        NotificationService notificationService)
    {
        _customerService = customerService;
        _deviceService = deviceService;
        _repairTicketService = repairTicketService;
        _saleService = saleService;
        _customerPaymentService = customerPaymentService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    partial void OnSelectedDeviceChanged(DeviceDto? value)
    {
        if (value is not null)
        {
            _ = LoadTicketsForDeviceAsync(value.Id);
        }
    }

    public async Task LoadAsync(int customerId)
    {
        IsLoading = true;
        try
        {
            Customer = await _customerService.GetByIdAsync(customerId);
            if (Customer is null)
            {
                ErrorMessage = "العميل غير موجود";
                return;
            }

            var devices = await _deviceService.GetByCustomerIdAsync(customerId);
            Devices = new ObservableCollection<DeviceDto>(devices);

            await LoadStatementAsync(customerId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadStatementAsync(int customerId)
    {
        var invoices = await _saleService.GetByCustomerIdAsync(customerId);
        var payments = await _customerPaymentService.GetByCustomerIdAsync(customerId);

        var raw = new List<(DateTime Date, string Description, decimal Delta, DateTime CreatedAt)>();
        foreach (var i in invoices)
            raw.Add((i.Date, $"فاتورة: {i.InvoiceNumber} (إجمالي {i.Total:N0}، مدفوع {i.PaidAmount:N0})", i.RemainingAmount, i.CreatedAt));
        foreach (var p in payments)
            raw.Add((p.Date, $"تحصيل: {p.Reference}".TrimEnd(':', ' '), -p.Amount, p.CreatedAt));

        var chronological = raw.OrderBy(e => e.Date).ThenBy(e => e.CreatedAt).ToList();
        decimal running = 0;
        var entries = new List<CustomerStatementEntryVm>();
        foreach (var e in chronological)
        {
            running += e.Delta;
            entries.Add(new CustomerStatementEntryVm(e.Date, e.Description, e.Delta, running));
        }
        entries.Reverse();

        Statement = new ObservableCollection<CustomerStatementEntryVm>(entries);
        OutstandingBalance = running;
    }

    [RelayCommand]
    private async Task CollectPaymentAsync()
    {
        if (Customer is null) return;
        if (CollectAmount <= 0)
        {
            ErrorMessage = "يرجى إدخال مبلغ أكبر من صفر";
            return;
        }

        IsLoading = true;
        try
        {
            await _customerPaymentService.CreateAsync(
                new CreateCustomerPaymentDto(DateTime.Today, Customer.Id, CollectAmount, CollectReference, ""));
            CollectAmount = 0;
            CollectReference = "";
            _notificationService.Show("تم تسجيل التحصيل بنجاح", NotificationType.Success);
            await LoadStatementAsync(Customer.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "تحصيل من العميل");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTicketsForDeviceAsync(int deviceId)
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var tickets = await _repairTicketService.GetByDeviceIdAsync(deviceId);
            SelectedDeviceTickets = new ObservableCollection<RepairTicketDto>(tickets);
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "تحميل تذاكر الجهاز في ملف العميل");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewTicketDetail(int ticketId)
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.RepairTicketDetailPage>();
        _navigationService.NavigateTo(page, "TicketId", ticketId);
    }

    [RelayCommand]
    private async Task CreateTicketForSelectedDeviceAsync()
    {
        ErrorMessage = "";

        if (SelectedDevice is null)
        {
            ErrorMessage = "يرجى اختيار جهاز من القائمة أولاً";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewTicketProblemDescription))
        {
            ErrorMessage = "يرجى إدخال وصف المشكلة";
            return;
        }

        IsLoading = true;
        try
        {
            var ticket = await _repairTicketService.CreateAsync(
                new CreateRepairTicketDto(SelectedDevice.Id, NewTicketProblemDescription, NewTicketReceptionNotes));

            NewTicketProblemDescription = "";
            NewTicketReceptionNotes = "";
            _notificationService.Show($"تم إنشاء تذكرة الصيانة بنجاح: {ticket.TicketNumber}", NotificationType.Success);

            await LoadTicketsForDeviceAsync(SelectedDevice.Id);
            TicketCreated?.Invoke(ticket.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "إنشاء تذكرة صيانة من ملف العميل");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
