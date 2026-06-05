namespace MRMS.Application.DTOs;

public record DashboardStatsDto(
    int TodayReceived,
    int TodayDelivered,
    int CurrentlyInRepair,
    int WaitingForParts,
    int TotalRepaired,
    int TotalDelivered,
    int TotalCustomers,
    int TotalDevices,
    int TotalTickets);
