using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IRepairTicketService
{
    Task<RepairTicketDto> CreateAsync(CreateRepairTicketDto dto);
    Task<RepairTicketDto?> GetByIdAsync(int id);
    Task<IEnumerable<RepairTicketDto>> GetByDeviceIdAsync(int deviceId);
    Task<bool> UpdateStatusAsync(UpdateRepairTicketStatusDto dto);
    Task<bool> UpdateRepairInfoAsync(UpdateRepairInfoDto dto);
    Task<IEnumerable<RepairTicketDto>> GetAllActiveAsync();
    Task<IEnumerable<RepairTicketDto>> GetAllAsync();
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<IEnumerable<RepairTicketPartDto>> GetPartsAsync(int ticketId);
    Task<RepairTicketPartDto> AddPartAsync(AddPartToTicketDto dto);
    Task RemovePartAsync(int ticketItemId);

    /// <summary>Assigns (or, with <paramref name="technicianId"/> null, unassigns) the technician
    /// responsible for a ticket. Requires <c>Permission.UpdateTicketStatus</c>.</summary>
    Task AssignTechnicianAsync(int ticketId, int? technicianId);

    /// <summary>Marks the ticket as notified (WhatsApp) that it's ready for pickup. Requires
    /// <c>Permission.UpdateTicketStatus</c>.</summary>
    Task MarkNotifiedAsync(int ticketId);

    /// <summary>Records a payment (deposit or partial/final) collected from the customer. Requires
    /// <c>Permission.CollectPayment</c>. No upper bound against Cost - a deposit can legitimately be
    /// collected before the final cost is known.</summary>
    Task<RepairTicketDto> CollectPaymentAsync(CollectPaymentDto dto);
}
