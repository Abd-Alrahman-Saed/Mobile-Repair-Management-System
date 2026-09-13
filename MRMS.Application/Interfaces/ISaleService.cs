using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface ISaleService
{
    Task<IEnumerable<SalesInvoiceDto>> GetAllAsync();
    Task<SalesInvoiceDto?> GetByIdAsync(int id);
    Task<IEnumerable<SalesInvoiceDto>> GetByCustomerIdAsync(int customerId);
    Task<SalesInvoiceDto> CreateAsync(CreateSalesInvoiceDto dto);
    Task VoidAsync(int id, string reason);
}
