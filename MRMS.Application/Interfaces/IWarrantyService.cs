using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IWarrantyService
{
    Task<WarrantyDto?> GetByRepairTicketIdAsync(int repairTicketId);
    Task<WarrantyDto> AssignAsync(AssignWarrantyDto dto);
}
