using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class PurchaseFormViewModel : ObservableObject
{
    private readonly IPurchaseService _purchaseService;
    private readonly IItemService _itemService;
    private readonly ISupplierService _supplierService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private int _selectedSupplierId;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private decimal _paidAmount;

    [ObservableProperty]
    private string _notes = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<SupplierDto> _suppliers = [];

    [ObservableProperty]
    private ObservableCollection<ItemDto> _items = [];

    [ObservableProperty]
    private ObservableCollection<PurchaseItemViewModel> _purchaseItems = [];

    [ObservableProperty]
    private int _selectedItemId;

    [ObservableProperty]
    private int _selectedQuantity = 1;

    [ObservableProperty]
    private decimal _selectedUnitCost;

    public decimal TotalCost => PurchaseItems.Sum(i => i.TotalCost);

    public PurchaseFormViewModel(
        IPurchaseService purchaseService,
        IItemService itemService,
        ISupplierService supplierService,
        AppNavigationService navigationService,
        NotificationService notificationService)
    {
        _purchaseService = purchaseService;
        _itemService = itemService;
        _supplierService = supplierService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var suppliers = await _supplierService.GetAllAsync();
        Suppliers = new ObservableCollection<SupplierDto>(suppliers);

        var items = await _itemService.GetAllAsync();
        Items = new ObservableCollection<ItemDto>(items);
        IsLoading = false;
    }

    [RelayCommand]
    private void AddItem()
    {
        if (SelectedItemId <= 0) return;
        var item = Items.FirstOrDefault(i => i.Id == SelectedItemId);
        if (item is null) return;

        PurchaseItems.Add(new PurchaseItemViewModel
        {
            ItemId = item.Id,
            ItemName = item.Name,
            Quantity = SelectedQuantity,
            UnitCost = SelectedUnitCost > 0 ? SelectedUnitCost : item.CostPrice
        });

        OnPropertyChanged(nameof(TotalCost));
    }

    [RelayCommand]
    private void RemoveItem(PurchaseItemViewModel item)
    {
        PurchaseItems.Remove(item);
        OnPropertyChanged(nameof(TotalCost));
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
        if (PurchaseItems.Count == 0)
        {
            ErrorMessage = "يرجى إضافة صنف واحد على الأقل";
            return;
        }

        IsLoading = true;
        try
        {
            var dto = new CreatePurchaseDto(
                Date, SelectedSupplierId, SelectedPaymentMethod, PaidAmount, Notes,
                PurchaseItems.Select(i => new CreatePurchaseItemDto(i.ItemId, i.Quantity, i.UnitCost)).ToList());

            await _purchaseService.CreateAsync(dto);
            _notificationService.Show("تم إضافة فاتورة المشتريات بنجاح", NotificationType.Success);
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

public partial class PurchaseItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _itemId;

    [ObservableProperty]
    private string _itemName = "";

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private decimal _unitCost;

    public decimal TotalCost => Quantity * UnitCost;
}
