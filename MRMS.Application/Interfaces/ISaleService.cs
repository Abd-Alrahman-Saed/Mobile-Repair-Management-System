using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface ISaleService
{
    Task<IEnumerable<SaleDto>> GetAllAsync();
    Task<SaleDto?> GetByIdAsync(int id);
    Task<SaleDto> CreateAsync(CreateSaleDto dto);
    Task UpdateAsync(UpdateSaleDto dto);
    Task DeleteAsync(int id);
}
