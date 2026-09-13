using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface ICustomerPaymentService
{
    Task<IEnumerable<CustomerPaymentDto>> GetAllAsync();
    Task<IEnumerable<CustomerPaymentDto>> GetByCustomerIdAsync(int customerId);
    Task<CustomerPaymentDto> CreateAsync(CreateCustomerPaymentDto dto);
    Task VoidAsync(int id, string reason);
}
