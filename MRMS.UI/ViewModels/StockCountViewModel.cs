using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

/// <summary>One row being counted: the book balance from the system vs. what was actually counted.</summary>
public partial class StockCountRowVm : ObservableObject
{
    public int ItemId { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal CostPrice { get; init; }
    public int BookStock { get; init; }

    [ObservableProperty]
    private int _actualCount;

    public int Difference => ActualCount - BookStock;

    partial void OnActualCountChanged(int value) => OnPropertyChanged(nameof(Difference));
}

public partial class StockCountViewModel : ObservableObject
{
    private readonly IItemService _itemService;
    private readonly IStockMovementService _stockMovementService;
    private readonly NotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<StockCountRowVm> _rows = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = "";

    public StockCountViewModel(IItemService itemService, IStockMovementService stockMovementService,
        NotificationService notificationService)
    {
        _itemService = itemService;
        _stockMovementService = stockMovementService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _itemService.GetAllAsync();
        // Service items carry no stock at all - nothing to count.
        Rows = new ObservableCollection<StockCountRowVm>(items
            .Where(i => i.Type != ItemType.Service)
            .Select(i => new StockCountRowVm
            {
                ItemId = i.Id,
                Code = i.Code,
                Name = i.Name,
                CostPrice = i.CostPrice ?? 0m,
                BookStock = i.CurrentStock ?? 0,
                ActualCount = i.CurrentStock ?? 0
            }));
        IsLoading = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = "";
        var changed = Rows.Where(r => r.Difference != 0).ToList();
        if (changed.Count == 0)
        {
            ErrorMessage = "لا يوجد فرق في أي صنف";
            return;
        }

        if (!await _notificationService.ConfirmAsync(
                $"سيتم تسجيل تسوية جرد لـ {changed.Count} صنف بفروقات مختلفة. متابعة؟"))
            return;

        IsLoading = true;
        try
        {
            foreach (var row in changed)
            {
                var diff = row.Difference;
                await _stockMovementService.CreateAsync(new CreateStockMovementDto(
                    DateTime.Today,
                    diff > 0 ? MovementType.AdjustmentIncrease : MovementType.AdjustmentDecrease,
                    row.ItemId, Math.Abs(diff), Math.Abs(diff) * row.CostPrice, null, "تسوية جرد"));
            }

            _notificationService.Show("تم حفظ الجرد بنجاح", NotificationType.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = UiError.Describe(ex, "حفظ الجرد");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
