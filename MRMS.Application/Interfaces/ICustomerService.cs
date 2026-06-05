using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
    Task<CustomerDto> UpdateAsync(UpdateCustomerDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasDevicesOrRepairsAsync(int customerId);
    Task<IEnumerable<CustomerDto>> SearchByNameAsync(string name);
}
