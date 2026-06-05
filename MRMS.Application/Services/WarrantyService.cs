using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class WarrantyService : IWarrantyService
{
    private readonly IUnitOfWork _uow;

    public WarrantyService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<WarrantyDto?> GetByRepairTicketIdAsync(int repairTicketId)
    {
        var warranties = await _uow.Warranties.FindAsync(
            w => w.RepairTicketId == repairTicketId);
        var warranty = warranties.FirstOrDefault();
        return warranty is null ? null : MapToDto(warranty);
    }

    public async Task<WarrantyDto> AssignAsync(AssignWarrantyDto dto)
    {
        var warranty = new Warranty
        {
            RepairTicketId = dto.RepairTicketId,
            DurationDays = dto.DurationDays,
            ExpiryDate = dto.ExpiryDate
        };
        await _uow.Warranties.AddAsync(warranty);
        await _uow.CompleteAsync();
        return MapToDto(warranty);
    }

    private static WarrantyDto MapToDto(Warranty w) =>
        new(w.Id, w.RepairTicketId, w.DurationDays, w.ExpiryDate, w.IsExpired);
}
