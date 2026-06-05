using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;

    public CustomerService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var customers = await _uow.Customers.GetAllAsync();
        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var customer = await _uow.Customers.GetByIdAsync(id);
        return customer is null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Customers.AddAsync(customer);
        await _uow.CompleteAsync();
        return MapToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(UpdateCustomerDto dto)
    {
        var customer = await _uow.Customers.GetByIdAsync(dto.Id);
        if (customer is null)
            throw new KeyNotFoundException($"Customer {dto.Id} not found");

        customer.FullName = dto.FullName;
        customer.PhoneNumber = dto.PhoneNumber;
        _uow.Customers.Update(customer);
        await _uow.CompleteAsync();
        return MapToDto(customer);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _uow.Customers.GetByIdAsync(id);
        if (customer is null) return false;

        _uow.Customers.Delete(customer);
        await _uow.CompleteAsync();
        return true;
    }

    public async Task<bool> HasDevicesOrRepairsAsync(int customerId)
    {
        var devices = await _uow.Devices.FindAsync(d => d.CustomerId == customerId);
        return devices.Any();
    }

    public async Task<IEnumerable<CustomerDto>> SearchByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Enumerable.Empty<CustomerDto>();

        var customers = await _uow.Customers.FindAsync(
            c => c.FullName.Contains(name));
        return customers.Select(MapToDto);
    }

    private static CustomerDto MapToDto(Customer c) =>
        new(c.Id, c.FullName, c.PhoneNumber, c.CreatedAt);
}
