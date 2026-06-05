using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class RepairTicketListViewModel : ObservableObject
{
    private readonly IRepairTicketService _repairTicketService;
    private readonly AppNavigationService _navigationService;

    public ObservableCollection<TicketStatusGroup> StatusGroups { get; }

    [ObservableProperty]
    private bool _isLoading;

    public RepairTicketListViewModel(
        IRepairTicketService repairTicketService,
        AppNavigationService navigationService)
    {
        _repairTicketService = repairTicketService;
        _navigationService = navigationService;

        StatusGroups =
        [
            new(TicketStatus.Received, "مستلم", new SolidColorBrush(Color.FromRgb(44, 62, 80))),
            new(TicketStatus.UnderInspection, "قيد الفحص", new SolidColorBrush(Color.FromRgb(52, 73, 94))),
            new(TicketStatus.WaitingForParts, "بانتظار القطع", new SolidColorBrush(Color.FromRgb(142, 68, 173))),
            new(TicketStatus.InRepair, "قيد الإصلاح", new SolidColorBrush(Color.FromRgb(41, 128, 185))),
            new(TicketStatus.Repaired, "تم الإصلاح", new SolidColorBrush(Color.FromRgb(39, 174, 96))),
            new(TicketStatus.Delivered, "تم التسليم", new SolidColorBrush(Color.FromRgb(127, 140, 141))),
            new(TicketStatus.Cancelled, "ملغي", new SolidColorBrush(Color.FromRgb(192, 57, 43))),
        ];
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var items = await _repairTicketService.GetAllAsync();

            foreach (var group in StatusGroups)
                group.Tickets.Clear();

            var grouped = items.GroupBy(t => t.Status);
            foreach (var g in grouped)
            {
                var group = StatusGroups.FirstOrDefault(sg => sg.Status == g.Key);
                if (group is null) continue;
                foreach (var ticket in g.OrderByDescending(t => t.ReceiveDate))
                    group.Tickets.Add(ticket);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenTicket(int id)
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.RepairTicketDetailPage>();
        _navigationService.NavigateTo(page, "TicketId", id);
    }
}
