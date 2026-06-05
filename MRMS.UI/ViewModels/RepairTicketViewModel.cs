using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class RepairTicketViewModel : ObservableObject
{
    private readonly IRepairTicketService _repairTicketService;
    private readonly IDeviceService _deviceService;
    private readonly ICustomerService _customerService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<RepairTicketDto> _tickets = [];

    [ObservableProperty]
    private int _selectedDeviceId;

    [ObservableProperty]
    private string _problemDescription = "";

    [ObservableProperty]
    private string _receptionNotes = "";

    [ObservableProperty]
    private string _repairDescription = "";

    [ObservableProperty]
    private decimal _cost;

    [ObservableProperty]
    private TicketStatus _selectedStatus = TicketStatus.Received;

    [ObservableProperty]
    private int _selectedStatusIndex;

    [ObservableProperty]
    private RepairTicketDto? _selectedTicket;

    partial void OnSelectedTicketChanged(RepairTicketDto? value)
    {
        if (value is not null)
        {
            SelectedStatus = value.Status;
            RepairDescription = value.RepairDescription;
            Cost = value.Cost;
        }
    }

    partial void OnSelectedStatusChanged(TicketStatus value)
    {
        var index = (int)value;
        if (SelectedStatusIndex != index)
            SelectedStatusIndex = index;
    }

    partial void OnSelectedStatusIndexChanged(int value)
    {
        var status = (TicketStatus)value;
        if (SelectedStatus != status)
            SelectedStatus = status;
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _showTicketForm;

    [ObservableProperty]
    private bool _showDetailPanel;

    [ObservableProperty]
    private ObservableCollection<PhotoDto> _photos = [];

    [ObservableProperty]
    private int _warrantyDuration;

    [ObservableProperty]
    private string _warrantyInfo = "";

    public event Action? TicketCreated;

    public RepairTicketViewModel(
        IRepairTicketService repairTicketService,
        IDeviceService deviceService,
        ICustomerService customerService,
        NotificationService notificationService)
    {
        _repairTicketService = repairTicketService;
        _deviceService = deviceService;
        _customerService = customerService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task CreateTicketAsync()
    {
        IsLoading = true;
        ErrorMessage = "";

        try
        {
            await _repairTicketService.CreateAsync(new CreateRepairTicketDto(
                SelectedDeviceId, ProblemDescription, ReceptionNotes));
            TicketCreated?.Invoke();
            ShowTicketForm = false;
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
    private async Task UpdateStatusAsync()
    {
        if (SelectedTicket is null) return;
        IsLoading = true;

        try
        {
            var result = await _repairTicketService.UpdateStatusAsync(
                new UpdateRepairTicketStatusDto(SelectedTicket.Id, SelectedStatus));
            if (!result)
                ErrorMessage = "لا يمكن تغيير الحالة - التذكرة في حالة نهائية";
            else
            {
                SelectedTicket = await _repairTicketService.GetByIdAsync(SelectedTicket.Id);
                var statusText = SelectedStatus switch
                {
                    TicketStatus.Received => "مستلم",
                    TicketStatus.UnderInspection => "تحت الفحص",
                    TicketStatus.WaitingForParts => "بانتظار القطع",
                    TicketStatus.InRepair => "قيد الإصلاح",
                    TicketStatus.Repaired => "تم الإصلاح",
                    TicketStatus.Delivered => "تم التسليم",
                    TicketStatus.Cancelled => "ملغي",
                    _ => SelectedStatus.ToString()
                };
                _notificationService.Show($"تم تحديث الحالة إلى: {statusText}", NotificationType.Success);
            }
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
    private async Task UpdateRepairInfoAsync()
    {
        if (SelectedTicket is null) return;
        IsLoading = true;

        try
        {
            var result = await _repairTicketService.UpdateRepairInfoAsync(
                new UpdateRepairInfoDto(SelectedTicket.Id, RepairDescription, Cost));
            if (result)
            {
                if (SelectedStatus != TicketStatus.Repaired)
                {
                    SelectedStatus = TicketStatus.Repaired;
                    await _repairTicketService.UpdateStatusAsync(
                        new UpdateRepairTicketStatusDto(SelectedTicket.Id, TicketStatus.Repaired));
                }
                SelectedTicket = await _repairTicketService.GetByIdAsync(SelectedTicket.Id);
                _notificationService.Show("تم حفظ بيانات الإصلاح بنجاح", NotificationType.Success);
            }
            else
                ErrorMessage = "لم يتم العثور على التذكرة";
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
    private async Task LoadByDeviceAsync(int deviceId)
    {
        IsLoading = true;
        var items = await _repairTicketService.GetByDeviceIdAsync(deviceId);
        Tickets = new ObservableCollection<RepairTicketDto>(items);
        IsLoading = false;
    }
}
