using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class ItemFormViewModel : ObservableObject
{
    private readonly IItemService _itemService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private int _editId;

    [ObservableProperty]
    private string _code = "";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private ItemType _selectedType = ItemType.Accessory;

    [ObservableProperty]
    private string _unit = "قطعة";

    [ObservableProperty]
    private decimal _costPrice;

    [ObservableProperty]
    private decimal _sellingPrice;

    [ObservableProperty]
    private int _reorderLevel;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditMode;

    public ItemFormViewModel(IItemService itemService, AppNavigationService navigationService, NotificationService notificationService)
    {
        _itemService = itemService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    public void SetEditData(ItemDto item)
    {
        _editId = item.Id;
        IsEditMode = true;
        Code = item.Code;
        Name = item.Name;
        SelectedType = item.Type;
        Unit = item.Unit;
        CostPrice = item.CostPrice ?? 0m;
        SellingPrice = item.SellingPrice;
        ReorderLevel = item.ReorderLevel;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "اسم الصنف مطلوب";
            return;
        }
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "كود الصنف مطلوب";
            return;
        }

        IsLoading = true;
        try
        {
            if (IsEditMode)
            {
                await _itemService.UpdateAsync(new UpdateItemDto(
                    _editId, Code, Name, SelectedType, Unit, CostPrice, SellingPrice, ReorderLevel));
                _notificationService.Show("تم تحديث الصنف بنجاح", NotificationType.Success);
            }
            else
            {
                await _itemService.CreateAsync(new CreateItemDto(
                    Code, Name, SelectedType, Unit, CostPrice, SellingPrice, ReorderLevel));
                _notificationService.Show("تم إضافة الصنف بنجاح", NotificationType.Success);
            }
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "حفظ الصنف");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
