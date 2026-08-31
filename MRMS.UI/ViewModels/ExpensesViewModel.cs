using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public enum ExpensePeriodFilter
{
    All,
    ThisMonth,
    LastMonth
}

public partial class ExpensesViewModel : ObservableObject
{
    private readonly IExpenseService _expenseService;
    private readonly AppNavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private List<ExpenseDto> _allExpenses = [];
    private const int PageSize = 20;

    [ObservableProperty]
    private ObservableCollection<ExpenseDto> _expenses = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private ExpensePeriodFilter _periodFilter = ExpensePeriodFilter.All;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)_filteredExpenses.Count / PageSize));
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
    public string PageInfo => $"صفحة {CurrentPage} من {TotalPages}";
    public decimal FilteredTotal => _filteredExpenses.Sum(e => e.Amount);

    private List<ExpenseDto> _filteredExpenses
    {
        get
        {
            var query = _allExpenses.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
                query = query.Where(e =>
                    e.Category.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    e.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

            if (PeriodFilter == ExpensePeriodFilter.ThisMonth)
            {
                var today = DateTime.Today;
                query = query.Where(e => e.Date.Year == today.Year && e.Date.Month == today.Month);
            }
            else if (PeriodFilter == ExpensePeriodFilter.LastMonth)
            {
                var lastMonth = DateTime.Today.AddMonths(-1);
                query = query.Where(e => e.Date.Year == lastMonth.Year && e.Date.Month == lastMonth.Month);
            }

            return query.ToList();
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        ApplyPaging();
    }

    partial void OnPeriodFilterChanged(ExpensePeriodFilter value)
    {
        CurrentPage = 1;
        ApplyPaging();
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
        CurrentPage = 1;
        var items = await _expenseService.GetAllAsync();
        _allExpenses = items.ToList();
        ApplyPaging();
        IsLoading = false;
    }

    [RelayCommand]
    private void NextPage()
    {
        if (HasNext) { CurrentPage++; ApplyPaging(); }
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (HasPrevious) { CurrentPage--; ApplyPaging(); }
    }

    private void ApplyPaging()
    {
        var source = _filteredExpenses;
        var paged = source.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        Expenses = new ObservableCollection<ExpenseDto>(paged);
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
        OnPropertyChanged(nameof(PageInfo));
        OnPropertyChanged(nameof(FilteredTotal));
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(int id)
    {
        var confirmed = await _notificationService.ConfirmAsync("هل أنت متأكد من حذف هذا المصروف؟");
        if (!confirmed) return;

        try { await _expenseService.DeleteAsync(id); }
        catch (Exception ex) { _notificationService.Show(ex.Message, NotificationType.Error); return; }

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
