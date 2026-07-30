using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class StockMovementsViewModel : ObservableObject
{
    private readonly IStockMovementService _movementService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private List<StockMovementDto> _allMovements = [];

    [ObservableProperty]
    private ObservableCollection<StockMovementDto> _movements = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = "";

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Movements = new ObservableCollection<StockMovementDto>(_allMovements);
        else
            Movements = new ObservableCollection<StockMovementDto>(
                _allMovements.Where(m =>
                    m.ItemName.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    m.Notes.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    public StockMovementsViewModel(IStockMovementService movementService, AppNavigationService navigationService, NotificationService notificationService)
    {
        _movementService = movementService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _movementService.GetAllAsync();
        _allMovements = items.ToList();
        Movements = new ObservableCollection<StockMovementDto>(_allMovements);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task DeleteMovementAsync(int id)
    {
        var confirm = System.Windows.MessageBox.Show(
            "هل أنت متأكد من حذف هذه الحركة؟", "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes)
            return;

        await _movementService.DeleteAsync(id);
        _notificationService.Show("تم حذف الحركة المخزنية بنجاح", NotificationType.Success);
        await LoadAsync();
    }

    [RelayCommand]
    private void AddMovement()
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.StockMovementFormPage>();
        _navigationService.NavigateTo(page);
    }
}
