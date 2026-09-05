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
}
