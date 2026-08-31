using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using MRMS.UI.Services;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class ExpensesPage : Page
{
    private readonly ExpensesViewModel _vm;
    private readonly AppNavigationService _navigationService;

    public ExpensesPage(ExpensesViewModel vm, AppNavigationService navigationService)
    {
        InitializeComponent();
        _vm = vm;
        _navigationService = navigationService;
        DataContext = vm;

        Loaded += async (_, _) =>
        {
            await vm.LoadCommand.ExecuteAsync(null);
            UpdatePeriodButtonStyles();
        };
    }

    private void AddExpense_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var page = App.ServiceProvider!.GetRequiredService<ExpenseFormPage>();
        _navigationService.NavigateTo(page);
    }

    private void PeriodAll_Click(object sender, System.Windows.RoutedEventArgs e)
        => SetPeriod(ExpensePeriodFilter.All);

    private void PeriodThisMonth_Click(object sender, System.Windows.RoutedEventArgs e)
        => SetPeriod(ExpensePeriodFilter.ThisMonth);

    private void PeriodLastMonth_Click(object sender, System.Windows.RoutedEventArgs e)
        => SetPeriod(ExpensePeriodFilter.LastMonth);

    private void SetPeriod(ExpensePeriodFilter filter)
    {
        _vm.PeriodFilter = filter;
        UpdatePeriodButtonStyles();
    }

    private void UpdatePeriodButtonStyles()
    {
        var primary = (System.Windows.Style)FindResource("PrimaryButton");
        var secondary = (System.Windows.Style)FindResource("SecondaryButton");

        AllPeriodButton.Style = _vm.PeriodFilter == ExpensePeriodFilter.All ? primary : secondary;
        ThisMonthButton.Style = _vm.PeriodFilter == ExpensePeriodFilter.ThisMonth ? primary : secondary;
        LastMonthButton.Style = _vm.PeriodFilter == ExpensePeriodFilter.LastMonth ? primary : secondary;
    }
}
