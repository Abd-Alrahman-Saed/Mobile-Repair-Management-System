using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;

namespace MRMS.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IRepairTicketService _repairTicketService;
    private readonly IDashboardService _dashboardService;

    public bool CanViewFinancials { get; }

    public DashboardViewModel(IRepairTicketService repairTicketService, IDashboardService dashboardService,
        IPermissionService permissions)
    {
        _repairTicketService = repairTicketService;
        _dashboardService = dashboardService;
        CanViewFinancials = permissions.Can(Permission.ViewReports);
    }

    [ObservableProperty]
    private DashboardStatsDto _stats = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    [ObservableProperty]
    private ObservableCollection<RepairTicketDto> _recentTickets = [];

    [ObservableProperty]
    private int _receivedCount;

    [ObservableProperty]
    private int _inRepairCount;

    [ObservableProperty]
    private int _repairedCount;

    [ObservableProperty]
    private int _cancelledCount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _periodLabel = "هذا الشهر";

    private DateTime _periodFrom;
    private DateTime _periodTo;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;

        _periodFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _periodTo = DateTime.Today;
        Stats = await _dashboardService.GetDashboardStatsAsync(_periodFrom, _periodTo);

        var allTickets = (await _repairTicketService.GetAllAsync()).ToList();

        RecentTickets = new ObservableCollection<RepairTicketDto>(
            allTickets.OrderByDescending(t => t.ReceiveDate).ThenByDescending(t => t.ReceiveTime).Take(6));

        ReceivedCount = allTickets.Count(t => t.Status == TicketStatus.Received);
        InRepairCount = allTickets.Count(t => t.Status is TicketStatus.UnderInspection or TicketStatus.WaitingForParts or TicketStatus.InRepair);
        RepairedCount = allTickets.Count(t => t.Status == TicketStatus.Repaired);
        CancelledCount = allTickets.Count(t => t.Status == TicketStatus.Cancelled);

        PeriodLabel = "هذا الشهر";
        IsLoading = false;
    }

    [RelayCommand]
    private async Task LastMonthAsync()
    {
        IsLoading = true;
        var firstOfLastMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        _periodFrom = firstOfLastMonth;
        _periodTo = firstOfLastMonth.AddMonths(1).AddDays(-1);
        Stats = await _dashboardService.GetDashboardStatsAsync(_periodFrom, _periodTo);
        PeriodLabel = "الشهر الماضي";
        IsLoading = false;
    }

    [RelayCommand]
    private async Task ThisMonthAsync()
    {
        await LoadAsync();
    }

    private static string FormatChange(double percent) => percent switch
    {
        > 0 => $"▲ {percent:F1}%",
        < 0 => $"▼ {Math.Abs(percent):F1}%",
        _ => "—"
    };

    public string RevenueChangeText => FormatChange(Stats.RevenueChangePercent);
    public string ExpensesChangeText => FormatChange(Stats.ExpensesChangePercent);
    public string NetProfitChangeText => FormatChange(Stats.NetProfitChangePercent);
}
