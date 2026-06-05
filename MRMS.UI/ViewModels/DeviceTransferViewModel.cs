using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;

namespace MRMS.UI.ViewModels;

public partial class DeviceTransferViewModel : ObservableObject
{
    private readonly IDeviceService _deviceService;
    private readonly ICustomerService _customerService;

    [ObservableProperty]
    private string _imei = "";

    [ObservableProperty]
    private DeviceDto? _device;

    [ObservableProperty]
    private int _selectedCustomerId;

    [ObservableProperty]
    private ObservableCollection<CustomerDto> _customers = [];

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _deviceFound;

    public event Action? TransferCompleted;

    public DeviceTransferViewModel(IDeviceService deviceService, ICustomerService customerService)
    {
        _deviceService = deviceService;
        _customerService = customerService;
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        var items = await _customerService.GetAllAsync();
        Customers = new ObservableCollection<CustomerDto>(items);
    }

    [RelayCommand]
    private async Task FindDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(Imei)) return;
        Device = await _deviceService.GetByImeiAsync(Imei);
        DeviceFound = Device is not null;
        ErrorMessage = DeviceFound ? "" : "الجهاز غير موجود";
    }

    [RelayCommand]
    private async Task TransferAsync()
    {
        if (Device is null) return;
        IsLoading = true;
        ErrorMessage = "";

        try
        {
            var result = await _deviceService.TransferOwnershipAsync(
                new TransferDeviceDto(Device.Id, SelectedCustomerId));
            if (result)
                TransferCompleted?.Invoke();
            else
                ErrorMessage = "فشل نقل الملكية";
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
}
