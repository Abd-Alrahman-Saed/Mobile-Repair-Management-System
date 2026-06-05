using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public enum DeviceRegistrationStep
{
    SearchImei,
    ExistingDevice,
    SearchCustomer,
    NewDeviceAndTicket
}

public partial class DeviceRegistrationViewModel : ObservableObject
{
    private readonly IDeviceService _deviceService;
    private readonly ICustomerService _customerService;
    private readonly IRepairTicketService _repairTicketService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSearchImeiStep))]
    [NotifyPropertyChangedFor(nameof(ShowExistingDeviceStep))]
    [NotifyPropertyChangedFor(nameof(ShowSearchCustomerStep))]
    [NotifyPropertyChangedFor(nameof(ShowNewDeviceAndTicketStep))]
    private DeviceRegistrationStep _currentStep = DeviceRegistrationStep.SearchImei;

    public bool ShowSearchImeiStep => CurrentStep == DeviceRegistrationStep.SearchImei;
    public bool ShowExistingDeviceStep => CurrentStep == DeviceRegistrationStep.ExistingDevice;
    public bool ShowSearchCustomerStep => CurrentStep == DeviceRegistrationStep.SearchCustomer;
    public bool ShowNewDeviceAndTicketStep => CurrentStep == DeviceRegistrationStep.NewDeviceAndTicket;

    [ObservableProperty]
    private string _imei = "";

    [ObservableProperty]
    private bool _isSearchingImei;

    [ObservableProperty]
    private DeviceDto? _foundDevice;

    [ObservableProperty]
    private CustomerDto? _foundDeviceCustomer;

    [ObservableProperty]
    private string _problemDescription = "";

    [ObservableProperty]
    private string _receptionNotes = "";

    [ObservableProperty]
    private string _customerNameSearch = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomerSearchResults))]
    private ObservableCollection<CustomerDto> _customerSearchResults = [];

    public bool HasCustomerSearchResults => CustomerSearchResults?.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCustomerFromSearch))]
    private CustomerDto? _selectedCustomerFromSearch;

    public bool HasSelectedCustomerFromSearch => SelectedCustomerFromSearch is not null;

    [ObservableProperty]
    private bool _isNewCustomer;

    [ObservableProperty]
    private string _newCustomerName = "";

    [ObservableProperty]
    private string _newCustomerPhone = "";

    [ObservableProperty]
    private CustomerDto? _selectedCustomerFinal;

    public ObservableCollection<string> Brands { get; } =
    [
        "Alcatel", "Amazon", "Apple", "Asus", "BBK", "BlackBerry", "Blackview",
        "Blu", "Cat", "Coolpad", "Doogee", "Energizer", "Fairphone", "Gionee",
        "Google", "HMD", "Honor", "HP", "HTC", "Huawei", "Infinix", "Infocus",
        "Intel", "Itel", "Kyocera", "Landvo", "LeEco", "Lenovo", "LG", "Meizu",
        "Micromax", "Microsoft", "Motorola", "NEC", "Netgear", "Nokia", "Nubia",
        "OnePlus", "Oppo", "Orange", "Panasonic", "Philips", "Plum", "Poco",
        "Razer", "Realme", "Samsung", "Sharp", "Siemens", "Sky", "Sony",
        "Sony Ericsson", "T-Mobile", "TCL", "Tecno", "Thomson", "Ulefone",
        "Vivo", "Vodafone", "Wiko", "Xiaomi", "XOLO", "Yota", "ZTE", "Zuk"
    ];

    [ObservableProperty]
    private string _brand = "";

    [ObservableProperty]
    private string _model = "";

    [ObservableProperty]
    private string _color = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private string _successMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public event Action? SaveCompleted;

    public DeviceRegistrationViewModel(
        IDeviceService deviceService,
        ICustomerService customerService,
        IRepairTicketService repairTicketService,
        NotificationService notificationService)
    {
        _deviceService = deviceService;
        _customerService = customerService;
        _repairTicketService = repairTicketService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task SearchImeiAsync()
    {
        if (string.IsNullOrWhiteSpace(Imei))
        {
            ErrorMessage = "يرجى إدخال رقم IMEI";
            return;
        }

        ErrorMessage = "";
        SuccessMessage = "";
        IsSearchingImei = true;
        ResetAllFields();

        try
        {
            var device = await _deviceService.GetByImeiAsync(Imei);

            if (device is not null)
            {
                FoundDevice = device;
                CurrentStep = DeviceRegistrationStep.ExistingDevice;

                if (device.CustomerId > 0)
                {
                    FoundDeviceCustomer = await _customerService.GetByIdAsync(device.CustomerId);
                }
            }
            else
            {
                CurrentStep = DeviceRegistrationStep.SearchCustomer;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ في البحث: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            IsSearchingImei = false;
        }
    }

    private bool _isSearching;
    private string _pendingSearch = "";

    [RelayCommand]
    private async Task SearchCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerNameSearch))
        {
            CustomerSearchResults = [];
            return;
        }

        if (_isSearching)
        {
            _pendingSearch = CustomerNameSearch;
            return;
        }

        _isSearching = true;
        ErrorMessage = "";

        try
        {
            var results = await _customerService.SearchByNameAsync(CustomerNameSearch);
            CustomerSearchResults = new ObservableCollection<CustomerDto>(results);

            if (!string.IsNullOrEmpty(_pendingSearch) && _pendingSearch != CustomerNameSearch)
            {
                CustomerNameSearch = _pendingSearch;
                _pendingSearch = "";
                await SearchCustomerAsync();
            }
            else
            {
                _pendingSearch = "";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ في البحث: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            _isSearching = false;
        }
    }

    [RelayCommand]
    private void UseSelectedCustomer()
    {
        if (SelectedCustomerFromSearch is null)
        {
            ErrorMessage = "يرجى اختيار عميل من القائمة";
            return;
        }

        IsNewCustomer = false;
        SelectedCustomerFinal = SelectedCustomerFromSearch;
        NewCustomerName = SelectedCustomerFromSearch.FullName;
        NewCustomerPhone = SelectedCustomerFromSearch.PhoneNumber;
        CurrentStep = DeviceRegistrationStep.NewDeviceAndTicket;
        ErrorMessage = "";
    }

    [RelayCommand]
    private void SwitchToNewCustomer()
    {
        IsNewCustomer = true;
        SelectedCustomerFinal = null;
        SelectedCustomerFromSearch = null;
        NewCustomerName = CustomerNameSearch;
        CurrentStep = DeviceRegistrationStep.NewDeviceAndTicket;
        ErrorMessage = "";
    }

    [RelayCommand]
    private void BackToCustomerSearch()
    {
        CurrentStep = DeviceRegistrationStep.SearchCustomer;
        ErrorMessage = "";
    }

    [RelayCommand]
    private async Task CreateTicketForExistingDeviceAsync()
    {
        if (FoundDevice is null)
        {
            ErrorMessage = "لم يتم العثور على جهاز";
            return;
        }

        if (string.IsNullOrWhiteSpace(ProblemDescription))
        {
            ErrorMessage = "يرجى إدخال وصف المشكلة";
            return;
        }

        IsLoading = true;
        ErrorMessage = "";
        SuccessMessage = "";

        try
        {
            var ticket = await _repairTicketService.CreateAsync(
                new CreateRepairTicketDto(FoundDevice.Id, ProblemDescription, ReceptionNotes));

            SuccessMessage = $"تم إنشاء التذكرة بنجاح: {ticket.TicketNumber}";
            _notificationService.Show($"تم إنشاء تذكرة الإصلاح بنجاح: {ticket.TicketNumber}", NotificationType.Success);
            ProblemDescription = "";
            ReceptionNotes = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RegisterNewDeviceAndTicketAsync()
    {
        int customerId;

        if (IsNewCustomer)
        {
            if (string.IsNullOrWhiteSpace(NewCustomerName) || NewCustomerName.Length > 100)
            {
                ErrorMessage = "اسم العميل يجب أن يكون بين 1 و 100 حرف";
                return;
            }

            if (NewCustomerPhone.Length > 20)
            {
                ErrorMessage = "رقم الهاتف يجب أن لا يتجاوز 20 رقم";
                return;
            }

            IsLoading = true;
            ErrorMessage = "";

            try
            {
                var newCustomer = await _customerService.CreateAsync(
                    new CreateCustomerDto(NewCustomerName, NewCustomerPhone));
                customerId = newCustomer.Id;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ في تسجيل العميل: {ex.InnerException?.Message ?? ex.Message}";
                IsLoading = false;
                return;
            }
        }
        else
        {
            if (SelectedCustomerFinal is null)
            {
                ErrorMessage = "يرجى اختيار عميل";
                return;
            }
            customerId = SelectedCustomerFinal.Id;
        }

        if (string.IsNullOrWhiteSpace(Imei) || Imei.Length > 15)
        {
            ErrorMessage = "IMEI يجب أن يكون بين 1 و 15 رقم";
            IsLoading = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(Brand) || Brand.Length > 50)
        {
            ErrorMessage = "الماركة يجب أن تكون بين 1 و 50 حرف";
            IsLoading = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(ProblemDescription))
        {
            ErrorMessage = "يرجى إدخال وصف المشكلة";
            IsLoading = false;
            return;
        }

        IsLoading = true;
        ErrorMessage = "";
        SuccessMessage = "";

        try
        {
            var device = await _deviceService.RegisterAsync(
                new RegisterDeviceDto(Imei, Brand, Model, Color, customerId));

            var ticket = await _repairTicketService.CreateAsync(
                new CreateRepairTicketDto(device.Id, ProblemDescription, ReceptionNotes));

            SuccessMessage = $"تم تسجيل الجهاز وإنشاء التذكرة بنجاح: {ticket.TicketNumber}";
            _notificationService.Show($"تم تسجيل الجهاز وإنشاء التذكرة بنجاح: {ticket.TicketNumber}", NotificationType.Success);
            SaveCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        CurrentStep = DeviceRegistrationStep.SearchImei;
        ResetAllFields();
        ErrorMessage = "";
        SuccessMessage = "";
    }

    private void ResetAllFields()
    {
        FoundDevice = null;
        FoundDeviceCustomer = null;
        ProblemDescription = "";
        ReceptionNotes = "";
        CustomerNameSearch = "";
        CustomerSearchResults = [];
        SelectedCustomerFromSearch = null;
        IsNewCustomer = false;
        NewCustomerName = "";
        NewCustomerPhone = "";
        SelectedCustomerFinal = null;
        Brand = "";
        Model = "";
        Color = "";
    }
}
