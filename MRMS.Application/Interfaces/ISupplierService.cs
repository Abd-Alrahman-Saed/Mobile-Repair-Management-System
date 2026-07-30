using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDto>> GetAllAsync();
    Task<SupplierDto?> GetByIdAsync(int id);
    Task<SupplierDto> CreateAsync(CreateSupplierDto dto);
    Task<SupplierDto> UpdateAsync(UpdateSupplierDto dto);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<SupplierDto>> SearchAsync(string query);
}
