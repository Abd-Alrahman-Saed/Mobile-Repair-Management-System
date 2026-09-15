using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Converters;
using MRMS.UI.Services;
using MRMS.UI.Views;

namespace MRMS.UI.ViewModels;

/// <summary>One nav entry. <see cref="Require"/> null = visible to everyone.</summary>
public record NavItem(string Section, string Title, Type PageType, Permission? Require);

public record NavGroup(string Header, IReadOnlyList<NavItem> Items);

public partial class ShellViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AppNavigationService _nav;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionService _permissions;
    private readonly ISearchService _searchService;
    private readonly IAlertService _alertService;
    private readonly DispatcherTimer _searchDebounce;

    // The complete menu. Filtered down to what the signed-in role may reach.
    private static readonly NavItem[] AllItems =
    [
        new("", "لوحة التحكم", typeof(DashboardPage), null),

        new("العملاء والأجهزة", "العملاء", typeof(CustomerListPage), null),
        new("العملاء والأجهزة", "تسجيل جهاز", typeof(DeviceRegistrationPage), Permission.ReceiveDevice),
        new("العملاء والأجهزة", "تذاكر الصيانة", typeof(RepairTicketListPage), null),
        new("العملاء والأجهزة", "نقل ملكية جهاز", typeof(DeviceTransferPage), Permission.ReceiveDevice),

        new("المخزون", "الأصناف", typeof(ItemsPage), null),
        new("المخزون", "الحركات المخزنية", typeof(StockMovementsPage), Permission.StockCount),
        new("المخزون", "كشف الرصيد المخزني", typeof(StockBalancePage), null),
        new("المخزون", "جرد المخزون", typeof(StockCountPage), Permission.StockCount),

        new("المبيعات والمشتريات", "المبيعات", typeof(SalesPage), null),
        new("المبيعات والمشتريات", "المشتريات", typeof(PurchasesPage), Permission.ManagePurchases),
        new("المبيعات والمشتريات", "الموردون", typeof(SuppliersPage), Permission.ManageSuppliers),
        new("المبيعات والمشتريات", "دفعات الموردين", typeof(SupplierPaymentsPage), Permission.ManageSuppliers),
        new("المبيعات والمشتريات", "المصروفات", typeof(ExpensesPage), Permission.ManageExpenses),

        new("التقارير المالية", "درج النقدية", typeof(CashRegisterPage), Permission.ViewCashRegister),
        new("التقارير المالية", "كشف حساب النقدية", typeof(CashbookPage), Permission.ViewReports),
        new("التقارير المالية", "قائمة الدخل", typeof(IncomeStatementPage), Permission.ViewReports),
        new("التقارير المالية", "التقرير الشهري", typeof(MonthlyReportPage), Permission.ViewReports),

        new("الإدارة", "المستخدمون", typeof(UsersPage), Permission.ManageUsers),
        new("الإدارة", "سجل التدقيق", typeof(AuditLogPage), Permission.ViewAuditLog),

        new("النظام", "بحث", typeof(SearchPage), null),
        new("النظام", "الإعدادات", typeof(SettingsPage), Permission.ManageSettings),
    ];

    public ObservableCollection<NavGroup> NavGroups { get; } = [];
    public ObservableCollection<SearchResultDto> SearchResults { get; } = [];
    public ObservableCollection<AlertDto> Alerts { get; } = [];

    public string UserName { get; }
    public string UserRoleText { get; }
    public string UserInitial { get; }

    [ObservableProperty] private string _searchQuery = "";
    [ObservableProperty] private bool _isSearchOpen;
    [ObservableProperty] private bool _isAlertsOpen;
    [ObservableProperty] private bool _hasAlerts;

    public event Action? LogoutRequested;

    public ShellViewModel(
        IServiceProvider serviceProvider,
        AppNavigationService nav,
        ICurrentUserService currentUser,
        IPermissionService permissions,
        ISearchService searchService,
        IAlertService alertService)
    {
        _serviceProvider = serviceProvider;
        _nav = nav;
        _currentUser = currentUser;
        _permissions = permissions;
        _searchService = searchService;
        _alertService = alertService;

        var user = _currentUser.User;
        UserName = user?.FullName is { Length: > 0 } fn ? fn : user?.Username ?? "";
        UserRoleText = user is null ? "" : UserRoleConverter.ToArabic(user.Role);
        UserInitial = UserName.Length > 0 ? UserName[..1] : "؟";

        BuildNav();

        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchDebounce.Tick += async (_, _) => { _searchDebounce.Stop(); await RunSearchAsync(); };
    }

    private void BuildNav()
    {
        var visible = AllItems.Where(i => i.Require is null || _permissions.Can(i.Require.Value));
        foreach (var group in visible.GroupBy(i => i.Section))
            NavGroups.Add(new NavGroup(group.Key, group.ToList()));
    }

    [RelayCommand]
    private void Navigate(NavItem? item)
    {
        if (item is null) return;
        IsSearchOpen = false;
        IsAlertsOpen = false;
        _nav.NavigateTo((Page)_serviceProvider.GetRequiredService(item.PageType));
    }

    [RelayCommand]
    private void Logout()
    {
        _currentUser.Clear();
        LogoutRequested?.Invoke();
    }

    partial void OnSearchQueryChanged(string value)
    {
        _searchDebounce.Stop();
        if (string.IsNullOrWhiteSpace(value))
        {
            SearchResults.Clear();
            IsSearchOpen = false;
            return;
        }
        _searchDebounce.Start();
    }

    private async Task RunSearchAsync()
    {
        var query = SearchQuery.Trim();
        if (query.Length == 0) return;

        var results = await _searchService.SearchAsync(query);
        SearchResults.Clear();
        foreach (var r in results.Take(8)) SearchResults.Add(r);
        IsSearchOpen = SearchResults.Count > 0;
    }

    [RelayCommand]
    private void OpenSearchResult(SearchResultDto? result)
    {
        if (result is null) return;
        IsSearchOpen = false;
        SearchQuery = "";

        if (result.Type == "تذكرة")
            _nav.NavigateTo(_serviceProvider.GetRequiredService<RepairTicketDetailPage>(), "TicketId", result.Id);
        else // جهاز / عميل
            _nav.NavigateTo(_serviceProvider.GetRequiredService<CustomerProfilePage>(),
                "CustomerId", result.CustomerId ?? result.Id);
    }

    [RelayCommand]
    private void ToggleAlerts() => IsAlertsOpen = !IsAlertsOpen;

    [RelayCommand]
    public async Task RefreshAlertsAsync()
    {
        try
        {
            var alerts = await _alertService.GetAlertsAsync();
            Alerts.Clear();
            foreach (var a in alerts) Alerts.Add(a);
            HasAlerts = Alerts.Count > 0;
        }
        catch
        {
            // Alerts are best-effort - never block the shell.
        }
    }
}
