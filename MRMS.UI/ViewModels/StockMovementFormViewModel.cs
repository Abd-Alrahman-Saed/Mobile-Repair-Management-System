using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class StockMovementFormViewModel : ObservableObject
{
    private readonly IStockMovementService _movementService;
    private readonly IItemService _itemService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private int _selectedItemId;

    [ObservableProperty]
    private ObservableCollection<ItemDto> _items = [];

    [ObservableProperty]
    private MovementType _selectedType = MovementType.CustomerReturn;

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private decimal _value;

    [ObservableProperty]
    private string _notes = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public StockMovementFormViewModel(
        IStockMovementService movementService,
        IItemService itemService,
        AppNavigationService navigationService,
        NotificationService notificationService)
    {
        _movementService = movementService;
        _itemService = itemService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    partial void OnSelectedItemIdChanged(int value)
    {
        var item = Items.FirstOrDefault(i => i.Id == value);
        if (item is not null)
            Value = item.CostPrice ?? 0m;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _itemService.GetAllAsync();
        Items = new ObservableCollection<ItemDto>(items);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";

        if (SelectedItemId <= 0)
        {
            ErrorMessage = "يرجى اختيار الصنف";
            return;
        }
        if (Quantity <= 0)
        {
            ErrorMessage = "الكمية يجب أن تكون أكبر من صفر";
            return;
        }

        IsLoading = true;
        try
        {
            var dto = new CreateStockMovementDto(Date, SelectedType, SelectedItemId, Quantity, Value, null, Notes);
            await _movementService.CreateAsync(dto);
            _notificationService.Show("تم تسجيل الحركة المخزنية بنجاح", NotificationType.Success);
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "حفظ حركة المخزون");
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
