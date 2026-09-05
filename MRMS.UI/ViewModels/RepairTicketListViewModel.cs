using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Resources;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

/// <summary>One entry in the technician filter dropdown. <c>Id = null</c> means "show everyone".</summary>
public record TechnicianFilterOption(int? Id, string Name);

public partial class RepairTicketListViewModel : ObservableObject
{
    private readonly IRepairTicketService _repairTicketService;
    private readonly IUserService _userService;
    private readonly AppNavigationService _navigationService;
    private List<RepairTicketDto> _allTickets = [];

    public ObservableCollection<TicketStatusGroup> StatusGroups { get; }

    [ObservableProperty]
    private ObservableCollection<TechnicianFilterOption> _technicianFilterOptions = [];

    [ObservableProperty]
    private int? _selectedTechnicianFilterId;

    [ObservableProperty]
    private bool _isLoading;

    public RepairTicketListViewModel(
        IRepairTicketService repairTicketService,
        IUserService userService,
        AppNavigationService navigationService)
    {
        _repairTicketService = repairTicketService;
        _userService = userService;
        _navigationService = navigationService;

        StatusGroups = new ObservableCollection<TicketStatusGroup>(
            TicketStatusPalette.Ordered.Select(s => new TicketStatusGroup(
                s, TicketStatusPalette.Label(s), TicketStatusPalette.ColumnBrush(s))));
    }

    partial void OnSelectedTechnicianFilterIdChanged(int? value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            _allTickets = (await _repairTicketService.GetAllAsync()).ToList();

            if (TechnicianFilterOptions.Count == 0)
            {
                var technicians = await _userService.GetTechniciansAsync();
                var options = new ObservableCollection<TechnicianFilterOption> { new(null, "كل الفنيين") };
                foreach (var t in technicians)
                    options.Add(new TechnicianFilterOption(t.Id, t.FullName));
                TechnicianFilterOptions = options;
            }

            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        foreach (var group in StatusGroups)
            group.Tickets.Clear();

        var tickets = SelectedTechnicianFilterId is { } technicianId
            ? _allTickets.Where(t => t.AssignedTechnicianId == technicianId)
            : _allTickets;

        foreach (var g in tickets.GroupBy(t => t.Status))
        {
            var group = StatusGroups.FirstOrDefault(sg => sg.Status == g.Key);
            if (group is null) continue;
            foreach (var ticket in g.OrderByDescending(t => t.ReceiveDate))
                group.Tickets.Add(ticket);
        }
    }

    [RelayCommand]
    private void OpenTicket(int id)
    {
        var page = App.ServiceProvider!.GetRequiredService<Views.RepairTicketDetailPage>();
        _navigationService.NavigateTo(page, "TicketId", id);
    }
}
