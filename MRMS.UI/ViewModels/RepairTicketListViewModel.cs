using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.Interfaces;
using MRMS.UI.Resources;
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

        StatusGroups = new ObservableCollection<TicketStatusGroup>(
            TicketStatusPalette.Ordered.Select(s => new TicketStatusGroup(
                s, TicketStatusPalette.Label(s), TicketStatusPalette.ColumnBrush(s))));
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
