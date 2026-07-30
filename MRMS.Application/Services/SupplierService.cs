using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _uow;

    public SupplierService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _uow.Suppliers.GetAllAsync();
        return suppliers.Select(MapToDto);
    }

    public async Task<SupplierDto?> GetByIdAsync(int id)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(id);
        return supplier is null ? null : MapToDto(supplier);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
    {
        var supplier = new Supplier
        {
            Code = dto.Code,
            Name = dto.Name,
            Phone = dto.Phone,
            OpeningBalance = dto.OpeningBalance,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Suppliers.AddAsync(supplier);
        await _uow.CompleteAsync();
        return MapToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(UpdateSupplierDto dto)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(dto.Id);
        if (supplier is null)
            throw new KeyNotFoundException($"Supplier {dto.Id} not found");

        supplier.Code = dto.Code;
        supplier.Name = dto.Name;
        supplier.Phone = dto.Phone;
        supplier.OpeningBalance = dto.OpeningBalance;
        supplier.Notes = dto.Notes;
        _uow.Suppliers.Update(supplier);
        await _uow.CompleteAsync();
        return MapToDto(supplier);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(id);
        if (supplier is null) return false;

        _uow.Suppliers.Delete(supplier);
        await _uow.CompleteAsync();
        return true;
    }

    public async Task<IEnumerable<SupplierDto>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<SupplierDto>();

        var suppliers = await _uow.Suppliers.FindAsync(
            s => s.Name.Contains(query) || s.Code.Contains(query) || s.Phone.Contains(query));
        return suppliers.Select(MapToDto);
    }

    private static SupplierDto MapToDto(Supplier s) =>
        new(s.Id, s.Code, s.Name, s.Phone, s.OpeningBalance, s.Notes, s.CreatedAt);
}
