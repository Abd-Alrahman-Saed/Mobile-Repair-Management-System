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

    public DashboardViewModel(IRepairTicketService repairTicketService)
    {
        _repairTicketService = repairTicketService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;

        Stats = await _repairTicketService.GetDashboardStatsAsync();

        var allTickets = (await _repairTicketService.GetAllAsync()).ToList();
        RecentTickets = new ObservableCollection<RepairTicketDto>(
            allTickets.OrderByDescending(t => t.ReceiveDate).ThenByDescending(t => t.ReceiveTime).Take(5));

        ReceivedCount = allTickets.Count(t => t.Status == TicketStatus.Received);
        InRepairCount = allTickets.Count(t => t.Status is TicketStatus.UnderInspection or TicketStatus.WaitingForParts or TicketStatus.InRepair);
        RepairedCount = allTickets.Count(t => t.Status == TicketStatus.Repaired);
        CancelledCount = allTickets.Count(t => t.Status == TicketStatus.Cancelled);

        IsLoading = false;
    }
}
