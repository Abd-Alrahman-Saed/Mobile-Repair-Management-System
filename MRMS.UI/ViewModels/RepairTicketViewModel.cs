using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core;
using MRMS.Core.Enums;
using MRMS.UI.Resources;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

/// <summary>One dot on the status timeline in the ticket detail screen.</summary>
public record TimelineStepVm(TicketStatus Status, string Label, bool IsPast);

public partial class RepairTicketViewModel : ObservableObject
{
    private readonly IRepairTicketService _repairTicketService;
    private readonly IDeviceService _deviceService;
    private readonly ICustomerService _customerService;
    private readonly IItemService _itemService;
    private readonly IUserService _userService;
    private readonly NotificationService _notificationService;
    private bool _suppressTechnicianChange;

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

    /// <summary>
    /// Cost is the single final amount charged to the customer (parts + labor combined).
    /// Expected profit = that amount minus the cost of parts actually consumed on this ticket.
    /// </summary>
    public decimal ExpectedProfit => Cost - PartsTotalCost;

    partial void OnCostChanged(decimal value) => OnPropertyChanged(nameof(ExpectedProfit));

    /// <summary>The status the user wants to move the ticket TO next - null when the ticket is in a
    /// terminal state (Delivered/Cancelled) and there is nothing left to pick.</summary>
    [ObservableProperty]
    private TicketStatus? _selectedStatus;

    [ObservableProperty]
    private ObservableCollection<TicketStatus> _allowedNextStatuses = [];

    [ObservableProperty]
    private ObservableCollection<TimelineStepVm> _timelineSteps = [];

    [ObservableProperty]
    private bool _isCancelled;

    [ObservableProperty]
    private ObservableCollection<UserDto> _availableTechnicians = [];

    [ObservableProperty]
    private int? _selectedTechnicianId;

    [ObservableProperty]
    private RepairTicketDto? _selectedTicket;

    partial void OnSelectedTicketChanged(RepairTicketDto? value)
    {
        if (value is not null)
        {
            AllowedNextStatuses = new ObservableCollection<TicketStatus>(TicketStateMachine.AllowedNext(value.Status));
            SelectedStatus = AllowedNextStatuses.FirstOrDefault();

            IsCancelled = value.Status == TicketStatus.Cancelled;
            TimelineSteps = BuildTimeline(value.Status);

            _suppressTechnicianChange = true;
            SelectedTechnicianId = value.AssignedTechnicianId;
            _suppressTechnicianChange = false;

            RepairDescription = value.RepairDescription;
            Cost = value.Cost;

            if (value.Parts is not null)
                Parts = new ObservableCollection<RepairTicketPartDto>(value.Parts);
            else
                Parts = [];

            OnPropertyChanged(nameof(PartsTotalCost));
            OnPropertyChanged(nameof(ExpectedProfit));
        }
    }

    private static ObservableCollection<TimelineStepVm> BuildTimeline(TicketStatus current)
    {
        var order = TicketStatusPalette.Ordered.Where(s => s != TicketStatus.Cancelled).ToList();
        var currentIndex = order.IndexOf(current);
        return new ObservableCollection<TimelineStepVm>(order.Select((s, i) =>
            new TimelineStepVm(s, TicketStatusPalette.Label(s), currentIndex >= 0 && i <= currentIndex)));
    }

    partial void OnSelectedTechnicianIdChanged(int? value)
    {
        if (_suppressTechnicianChange) return;
        _ = AssignTechnicianAsync(value);
    }

    private async Task AssignTechnicianAsync(int? technicianId)
    {
        if (SelectedTicket is null) return;
        try
        {
            await _repairTicketService.AssignTechnicianAsync(SelectedTicket.Id, technicianId);
            var updated = await _repairTicketService.GetByIdAsync(SelectedTicket.Id);
            if (updated is not null)
                SelectedTicket = updated;
            _notificationService.Show("تم تحديث الفني المسؤول", NotificationType.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "تخصيص فني");
            // Roll the picker back to what the ticket actually has, since the assignment didn't take.
            _suppressTechnicianChange = true;
            SelectedTechnicianId = SelectedTicket.AssignedTechnicianId;
            _suppressTechnicianChange = false;
        }
    }

    [RelayCommand]
    private async Task LoadTechniciansAsync()
    {
        var techs = await _userService.GetTechniciansAsync();
        AvailableTechnicians = new ObservableCollection<UserDto>(techs);
    }

    [RelayCommand]
    private void UnassignTechnician() => SelectedTechnicianId = null;

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

    [ObservableProperty]
    private ObservableCollection<RepairTicketPartDto> _parts = [];

    [ObservableProperty]
    private ObservableCollection<ItemDto> _availableItems = [];

    [ObservableProperty]
    private int _selectedPartItemId;

    [ObservableProperty]
    private int _partQuantity = 1;

    [ObservableProperty]
    private decimal _partUnitCost;

    public decimal PartsTotalCost => Parts.Sum(p => p.TotalCost);

    public event Action? TicketCreated;

    public RepairTicketViewModel(
        IRepairTicketService repairTicketService,
        IDeviceService deviceService,
        ICustomerService customerService,
        IItemService itemService,
        IUserService userService,
        NotificationService notificationService)
    {
        _repairTicketService = repairTicketService;
        _deviceService = deviceService;
        _customerService = customerService;
        _itemService = itemService;
        _userService = userService;
        _notificationService = notificationService;
    }

    partial void OnSelectedPartItemIdChanged(int value)
    {
        var item = AvailableItems.FirstOrDefault(i => i.Id == value);
        if (item is not null)
            PartUnitCost = item.CostPrice ?? 0m;
    }

    public void AddAndSelectNewPart(ItemDto item)
    {
        AvailableItems.Add(item);
        SelectedPartItemId = item.Id;
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        var items = await _itemService.GetAllAsync();
        AvailableItems = new ObservableCollection<ItemDto>(items);
    }

    [RelayCommand]
    private async Task AddPartAsync()
    {
        if (SelectedTicket is null) return;
        if (SelectedPartItemId <= 0)
        {
            ErrorMessage = "يرجى اختيار قطعة";
            return;
        }

        IsLoading = true;
        try
        {
            var part = await _repairTicketService.AddPartAsync(new AddPartToTicketDto(
                SelectedTicket.Id, SelectedPartItemId, PartQuantity, PartUnitCost));
            Parts.Add(part);
            SelectedPartItemId = 0;
            PartQuantity = 1;
            PartUnitCost = 0;
            ErrorMessage = "";
            OnPropertyChanged(nameof(PartsTotalCost));
            OnPropertyChanged(nameof(ExpectedProfit));

            var updated = await _repairTicketService.GetByIdAsync(SelectedTicket.Id);
            if (updated is not null)
                SelectedTicket = updated;
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "عملية على تذكرة الصيانة");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RemovePartAsync(RepairTicketPartDto part)
    {
        if (SelectedTicket is null) return;

        var confirm = System.Windows.MessageBox.Show(
            "هل أنت متأكد من حذف هذه القطعة؟", "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes)
            return;

        IsLoading = true;
        try
        {
            await _repairTicketService.RemovePartAsync(part.Id);
            Parts.Remove(part);
            OnPropertyChanged(nameof(PartsTotalCost));
            OnPropertyChanged(nameof(ExpectedProfit));

            var updated = await _repairTicketService.GetByIdAsync(SelectedTicket.Id);
            if (updated is not null)
                SelectedTicket = updated;
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "عملية على تذكرة الصيانة");
        }
        finally
        {
            IsLoading = false;
        }
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
            ErrorMessage = UiError.Describe(ex, "عملية على تذكرة الصيانة");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UpdateStatusAsync()
    {
        if (SelectedTicket is null || SelectedStatus is not { } newStatus)
            return;
        IsLoading = true;

        try
        {
            var result = await _repairTicketService.UpdateStatusAsync(
                new UpdateRepairTicketStatusDto(SelectedTicket.Id, newStatus));
            if (!result)
                ErrorMessage = "لا يمكن تغيير الحالة - هذا الانتقال غير مسموح من الحالة الحالية";
            else
            {
                SelectedTicket = await _repairTicketService.GetByIdAsync(SelectedTicket.Id);
                _notificationService.Show(
                    $"تم تحديث الحالة إلى: {TicketStatusPalette.Label(newStatus)}", NotificationType.Success);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "عملية على تذكرة الصيانة");
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
                // Saving repair notes/cost never changes the ticket's status by itself - that's a
                // separate, explicit action from the "تحديث الحالة" control below.
                SelectedTicket = await _repairTicketService.GetByIdAsync(SelectedTicket.Id);
                _notificationService.Show("تم حفظ بيانات الصيانة بنجاح", NotificationType.Success);
            }
            else
                ErrorMessage = "لم يتم العثور على التذكرة";
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "عملية على تذكرة الصيانة");
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
