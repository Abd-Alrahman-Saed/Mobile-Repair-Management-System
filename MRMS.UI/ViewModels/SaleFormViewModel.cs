using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class SaleFormViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
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
    private int _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _costPrice;

    [ObservableProperty]
    private string _customerName = "";

    [ObservableProperty]
    private string _notes = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _scanCode = "";

    [ObservableProperty]
    private int _selectedItemCurrentStock;

    public decimal TotalPrice => Quantity * UnitPrice;
    public decimal Profit => TotalPrice - (Quantity * CostPrice);

    /// <summary>Raised after a barcode scan successfully resolves an item, so the view can move focus to Quantity.</summary>
    public event Action? ItemScanned;

    public SaleFormViewModel(
        ISaleService saleService,
        IItemService itemService,
        AppNavigationService navigationService,
        NotificationService notificationService)
    {
        _saleService = saleService;
        _itemService = itemService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    partial void OnSelectedItemIdChanged(int value)
    {
        var item = Items.FirstOrDefault(i => i.Id == value);
        if (item is not null)
        {
            UnitPrice = item.SellingPrice;
            CostPrice = item.CostPrice;
            SelectedItemCurrentStock = item.CurrentStock;
        }
        else
        {
            SelectedItemCurrentStock = 0;
        }
    }

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(Profit));
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(Profit));
    }

    partial void OnCostPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(Profit));
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
    private async Task ScanItemAsync()
    {
        var code = ScanCode.Trim();
        ScanCode = "";
        if (string.IsNullOrWhiteSpace(code)) return;

        ErrorMessage = "";
        var item = await _itemService.GetByCodeAsync(code);
        if (item is null)
        {
            ErrorMessage = $"لم يتم العثور على صنف بالكود: {code}";
            return;
        }

        if (Items.All(i => i.Id != item.Id))
            Items.Add(item);

        SelectedItemId = item.Id;
        ItemScanned?.Invoke();
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
            var dto = new CreateSaleDto(Date, SelectedItemId, Quantity, UnitPrice, CostPrice, CustomerName, Notes);
            await _saleService.CreateAsync(dto);
            _notificationService.Show("تم تسجيل عملية البيع بنجاح", NotificationType.Success);
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "تسجيل عملية البيع");
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
