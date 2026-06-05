using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using MRMS.Application.DTOs;
using MRMS.Core.Enums;

namespace MRMS.UI.ViewModels;

public class TicketStatusGroup : ObservableObject
{
    public TicketStatus Status { get; }
    public string DisplayName { get; }
    public SolidColorBrush HeaderBrush { get; }
    public ObservableCollection<RepairTicketDto> Tickets { get; } = [];

    public int Count => Tickets.Count;

    public TicketStatusGroup(TicketStatus status, string displayName, SolidColorBrush headerBrush)
    {
        Status = status;
        DisplayName = displayName;
        HeaderBrush = headerBrush;
        Tickets.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Count));
    }
}
