using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class CustomerProfileViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;
    private readonly IDeviceService _deviceService;
    private readonly IRepairTicketService _repairTicketService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private CustomerDto? _customer;

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
        AppNavigationService navigationService,
        NotificationService notificationService)
    {
        _customerService = customerService;
        _deviceService = deviceService;
        _repairTicketService = repairTicketService;
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
