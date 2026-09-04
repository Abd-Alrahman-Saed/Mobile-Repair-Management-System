using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface ISupplierPaymentService
{
    Task<IEnumerable<SupplierPaymentDto>> GetAllAsync();
    Task<IEnumerable<SupplierPaymentDto>> GetBySupplierIdAsync(int supplierId);
    Task<SupplierPaymentDto> CreateAsync(CreateSupplierPaymentDto dto);
    Task VoidAsync(int id, string reason);
}
