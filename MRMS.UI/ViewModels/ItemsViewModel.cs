using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class ItemsViewModel : ObservableObject
{
    private readonly IItemService _itemService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private List<ItemDto> _allItems = [];

    public bool CanViewCost { get; }

    [ObservableProperty]
    private ObservableCollection<ItemDto> _items = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReorderItems))]
    private ObservableCollection<ItemDto> _reorderItems = [];

    public bool HasReorderItems => ReorderItems.Count > 0;

    [ObservableProperty]
    private decimal _totalStockValue;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private int _outOfStockCount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = "";

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Items = new ObservableCollection<ItemDto>(_allItems);
        else
            Items = new ObservableCollection<ItemDto>(
                _allItems.Where(i =>
                    i.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    i.Code.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    public ItemsViewModel(IItemService itemService, AppNavigationService navigationService,
        NotificationService notificationService, IPermissionService permissions)
    {
        _itemService = itemService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        CanViewCost = permissions.Can(Permission.ViewCostPrice);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _itemService.GetAllAsync();
        _allItems = items.ToList();
        Items = new ObservableCollection<ItemDto>(_allItems);

        // Trackable items (Service items carry no stock, so a null CurrentStock never counts here).
        var trackable = _allItems.Where(i => i.CurrentStock.HasValue).ToList();
        TotalStockValue = CanViewCost ? trackable.Sum(i => (i.CostPrice ?? 0m) * i.CurrentStock!.Value) : 0;
        LowStockCount = trackable.Count(i => i.CurrentStock <= i.ReorderLevel);
        OutOfStockCount = trackable.Count(i => i.CurrentStock <= 0);
        ReorderItems = new ObservableCollection<ItemDto>(
            trackable.Where(i => i.CurrentStock <= i.ReorderLevel).OrderBy(i => i.CurrentStock));

        IsLoading = false;
    }

    [RelayCommand]
    private void RequestPurchase(int itemId)
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.PurchaseFormPage>();
        _navigationService.NavigateTo(page, "PresetItemId", itemId);
    }

    [RelayCommand]
    private async Task DeleteItemAsync(int id)
    {
        var confirm = System.Windows.MessageBox.Show(
            "هل أنت متأكد من حذف هذا الصنف؟", "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes)
            return;

        await _itemService.DeleteAsync(id);
        _notificationService.Show("تم حذف الصنف بنجاح", NotificationType.Success);
        await LoadAsync();
    }

    [RelayCommand]
    private void AddItem()
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.ItemFormPage>();
        _navigationService.NavigateTo(page);
    }

    [RelayCommand]
    private void EditItem(int id)
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.ItemFormPage>();
        _navigationService.NavigateTo(page, "EditItemId", id);
    }
}
