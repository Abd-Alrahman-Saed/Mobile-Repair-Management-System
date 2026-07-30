using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class ExpensesViewModel : ObservableObject
{
    private readonly IExpenseService _expenseService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private List<ExpenseDto> _allExpenses = [];

    [ObservableProperty]
    private ObservableCollection<ExpenseDto> _expenses = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = "";

    partial void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Expenses = new ObservableCollection<ExpenseDto>(_allExpenses);
        else
            Expenses = new ObservableCollection<ExpenseDto>(
                _allExpenses.Where(e =>
                    e.Category.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    e.Description.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    public ExpensesViewModel(IExpenseService expenseService, AppNavigationService navigationService, NotificationService notificationService)
    {
        _expenseService = expenseService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var items = await _expenseService.GetAllAsync();
        _allExpenses = items.ToList();
        Expenses = new ObservableCollection<ExpenseDto>(_allExpenses);
        IsLoading = false;
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(int id)
    {
        var confirm = System.Windows.MessageBox.Show(
            "هل أنت متأكد من حذف هذا المصروف؟", "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes)
            return;

        await _expenseService.DeleteAsync(id);
        _notificationService.Show("تم حذف المصروف بنجاح", NotificationType.Success);
        await LoadAsync();
    }

    [RelayCommand]
    private void AddExpense()
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.ExpenseFormPage>();
        _navigationService.NavigateTo(page);
    }
}
