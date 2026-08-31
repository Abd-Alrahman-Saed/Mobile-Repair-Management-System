using MRMS.Application.DTOs;
using MRMS.Core.Enums;

namespace MRMS.Application.Interfaces;

public interface IItemService
{
    Task<IEnumerable<ItemDto>> GetAllAsync();
    Task<ItemDto?> GetByIdAsync(int id);
    Task<ItemDto?> GetByCodeAsync(string code);
    Task<ItemDto> CreateAsync(CreateItemDto dto);
    Task<ItemDto> UpdateAsync(UpdateItemDto dto);
    Task UpdatePricesAsync(int id, decimal costPrice, decimal sellingPrice);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<ItemDto>> SearchAsync(string query);
    Task<IEnumerable<ItemDto>> GetLowStockAsync();
}
