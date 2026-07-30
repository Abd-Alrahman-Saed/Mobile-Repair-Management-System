using MRMS.Application.DTOs;
using MRMS.Core.Enums;

namespace MRMS.Application.Interfaces;

public interface IItemService
{
    Task<IEnumerable<ItemDto>> GetAllAsync();
    Task<ItemDto?> GetByIdAsync(int id);
    Task<ItemDto> CreateAsync(CreateItemDto dto);
    Task<ItemDto> UpdateAsync(UpdateItemDto dto);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<ItemDto>> SearchAsync(string query);
    Task<IEnumerable<ItemDto>> GetLowStockAsync();
}
