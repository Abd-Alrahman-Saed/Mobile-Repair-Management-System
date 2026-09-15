using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

/// <summary>One line being built up in the invoice form, before it's saved.</summary>
public partial class SalesInvoiceLineVm : ObservableObject
{
    public int ItemId { get; init; }
    public string ItemName { get; init; } = "";
    public decimal UnitCost { get; init; }

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _unitPrice;

    public decimal LineTotal => Quantity * UnitPrice;

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}

public partial class SaleFormViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly IItemService _itemService;
    private readonly ICustomerService _customerService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;

    public bool CanViewProfit { get; }

    /// <summary>Set by the view: show the owner-approval dialog for the given message; return the
    /// approver's user id, or null if cancelled.</summary>
    public Func<string, int?> RequestApproval { get; set; } = _ => null;

    /// <summary>Raised after a successful save, so the view can open the print preview.</summary>
    public event Action<SalesInvoiceDto>? InvoiceCreated;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private ObservableCollection<ItemDto> _items = [];

    [ObservableProperty]
    private ObservableCollection<CustomerDto> _customers = [];

    [ObservableProperty]
    private ObservableCollection<SalesInvoiceLineVm> _lines = [];

    // ===== Current line being entered =====

    [ObservableProperty]
    private int _selectedItemId;

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _costPrice;

    [ObservableProperty]
    private int _selectedItemCurrentStock;

    [ObservableProperty]
    private string _scanCode = "";

    // ===== Invoice-level fields =====

    [ObservableProperty]
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private int? _selectedCustomerId;

    [ObservableProperty]
    private string _customerName = "";

    [ObservableProperty]
    private decimal _discountAmount;

    [ObservableProperty]
    private decimal _paidAmount;

    [ObservableProperty]
    private string _notes = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isLoading;

    public decimal Subtotal => Lines.Sum(l => l.LineTotal);
    public decimal Total => Subtotal - DiscountAmount;
    public decimal Remaining => Total - PaidAmount;
    public decimal Profit => Lines.Sum(l => l.LineTotal - l.Quantity * l.UnitCost) - DiscountAmount;

    /// <summary>Raised after a barcode scan successfully resolves an item, so the view can move focus to Quantity.</summary>
    public event Action? ItemScanned;

    public SaleFormViewModel(
        ISaleService saleService,
        IItemService itemService,
        ICustomerService customerService,
        AppNavigationService navigationService,
        NotificationService notificationService,
        IPermissionService permissions)
    {
        _saleService = saleService;
        _itemService = itemService;
        _customerService = customerService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        CanViewProfit = permissions.Can(Permission.ViewProfit);
    }

    partial void OnSelectedItemIdChanged(int value)
    {
        var item = Items.FirstOrDefault(i => i.Id == value);
        if (item is not null)
        {
            UnitPrice = item.SellingPrice;
            CostPrice = item.CostPrice ?? 0m;
            SelectedItemCurrentStock = item.CurrentStock ?? 0;
        }
        else
        {
            SelectedItemCurrentStock = 0;
        }
    }

    partial void OnDiscountAmountChanged(decimal value) => RecalculateTotals();
    partial void OnPaidAmountChanged(decimal value) => OnPropertyChanged(nameof(Remaining));

    partial void OnPaymentMethodChanged(PaymentMethod value)
    {
        // Sensible defaults - a cash sale is fully paid up front, a credit sale starts unpaid; the
        // user can still edit PaidAmount afterward either way (a partial cash deposit, etc.).
        PaidAmount = value == PaymentMethod.Cash ? Total : 0m;
    }

    partial void OnSelectedCustomerIdChanged(int? value)
    {
        var customer = Customers.FirstOrDefault(c => c.Id == value);
        if (customer is not null)
            CustomerName = customer.FullName;
    }

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Remaining));
        OnPropertyChanged(nameof(Profit));
        if (PaymentMethod == PaymentMethod.Cash)
            PaidAmount = Total;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _itemService.GetAllAsync();
        Items = new ObservableCollection<ItemDto>(items);
        var customers = await _customerService.GetAllAsync();
        Customers = new ObservableCollection<CustomerDto>(customers);
        IsLoading = false;
    }

    [RelayCommand]
    private void AddLine()
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
        if (UnitPrice <= 0)
        {
            ErrorMessage = "السعر يجب أن يكون أكبر من صفر";
            return;
        }

        AddOrIncrementLine(SelectedItemId, Quantity, UnitPrice, CostPrice);

        SelectedItemId = 0;
        Quantity = 1;
        UnitPrice = 0;
        CostPrice = 0;
    }

    /// <summary>Adds a new line, or - if the item is already on the invoice - increases its quantity
    /// instead of creating a duplicate row.</summary>
    private void AddOrIncrementLine(int itemId, int quantity, decimal unitPrice, decimal unitCost)
    {
        var existing = Lines.FirstOrDefault(l => l.ItemId == itemId);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            var item = Items.FirstOrDefault(i => i.Id == itemId);
            Lines.Add(new SalesInvoiceLineVm
            {
                ItemId = itemId,
                ItemName = item?.Name ?? "",
                UnitCost = unitCost,
                Quantity = quantity,
                UnitPrice = unitPrice
            });
        }
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveLine(SalesInvoiceLineVm line)
    {
        Lines.Remove(line);
        RecalculateTotals();
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

        // A scan adds the item to the invoice immediately (qty 1, or +1 if already on it) - no extra
        // click needed, unlike the manual combo-box + "إضافة سطر" path above.
        AddOrIncrementLine(item.Id, 1, item.SellingPrice, item.CostPrice ?? 0m);
        ItemScanned?.Invoke();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";

        if (Lines.Count == 0)
        {
            ErrorMessage = "يرجى إضافة صنف واحد على الأقل للفاتورة";
            return;
        }
        if (PaymentMethod == PaymentMethod.Credit && SelectedCustomerId is null)
        {
            ErrorMessage = "البيع الآجل يتطلب اختيار عميل مسجّل";
            return;
        }

        IsLoading = true;
        try
        {
            await CreateInvoiceWithApprovalAsync(approvedByUserId: null);
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "تسجيل الفاتورة");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreateInvoiceWithApprovalAsync(int? approvedByUserId)
    {
        try
        {
            var dto = new CreateSalesInvoiceDto(
                Date, SelectedCustomerId, CustomerName, PaymentMethod,
                Lines.Select(l => new CreateSalesInvoiceLineDto(l.ItemId, l.Quantity, l.UnitPrice)).ToList(),
                DiscountAmount, PaidAmount, Notes, approvedByUserId);

            var invoice = await _saleService.CreateAsync(dto);
            _notificationService.Show($"تم تسجيل الفاتورة {invoice.InvoiceNumber} بنجاح", NotificationType.Success);
            InvoiceCreated?.Invoke(invoice);
            _navigationService.GoBack();
        }
        catch (InvalidOperationException ex) when (approvedByUserId is null && ex.Message.Contains("موافقة المالك"))
        {
            var approverId = RequestApproval(ex.Message);
            if (approverId is null)
            {
                ErrorMessage = ex.Message;
                return;
            }
            await CreateInvoiceWithApprovalAsync(approverId);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }
}
