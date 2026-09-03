using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class RepairTicketCreateViewModel : ObservableObject
{
    private readonly IRepairTicketService _repairTicketService;
    private readonly IDeviceService _deviceService;

    [ObservableProperty]
    private string _imei = "";

    [ObservableProperty]
    private DeviceDto? _foundDevice;

    [ObservableProperty]
    private bool _deviceFound;

    [ObservableProperty]
    private string _problemDescription = "";

    [ObservableProperty]
    private string _receptionNotes = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public event Action<int>? TicketCreated;

    public RepairTicketCreateViewModel(
        IRepairTicketService repairTicketService,
        IDeviceService deviceService)
    {
        _repairTicketService = repairTicketService;
        _deviceService = deviceService;
    }

    [RelayCommand]
    private async Task FindDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(Imei)) return;
        ErrorMessage = "";
        DeviceFound = false;
        FoundDevice = null;

        var device = await _deviceService.GetByImeiAsync(Imei);
        if (device is null)
        {
            ErrorMessage = "الجهاز غير موجود";
            return;
        }
        FoundDevice = device;
        DeviceFound = true;
    }

    [RelayCommand]
    private async Task CreateTicketAsync()
    {
        IsLoading = true;
        ErrorMessage = "";

        if (!DeviceFound || FoundDevice is null)
        {
            ErrorMessage = "يرجى البحث عن الجهاز أولاً";
            IsLoading = false;
            return;
        }
        if (string.IsNullOrWhiteSpace(ProblemDescription))
        {
            ErrorMessage = "يرجى إدخال وصف المشكلة";
            IsLoading = false;
            return;
        }

        try
        {
            var ticket = await _repairTicketService.CreateAsync(
                new CreateRepairTicketDto(FoundDevice.Id, ProblemDescription, ReceptionNotes));
            TicketCreated?.Invoke(ticket.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "إنشاء تذكرة الصيانة");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
