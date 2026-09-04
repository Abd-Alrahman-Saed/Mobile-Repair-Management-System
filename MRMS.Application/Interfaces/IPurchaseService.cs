using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IPurchaseService
{
    Task<IEnumerable<PurchaseDto>> GetAllAsync();
    Task<PurchaseDto?> GetByIdAsync(int id);
    Task<PurchaseDto> CreateAsync(CreatePurchaseDto dto);
    Task UpdateAsync(UpdatePurchaseDto dto);
    Task VoidAsync(int id, string reason);
}
