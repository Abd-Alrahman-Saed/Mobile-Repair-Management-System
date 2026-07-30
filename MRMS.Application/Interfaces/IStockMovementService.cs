using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IStockMovementService
{
    Task<IEnumerable<StockMovementDto>> GetAllAsync();
    Task<IEnumerable<StockMovementDto>> GetByItemIdAsync(int itemId);
    Task<StockMovementDto> CreateAsync(CreateStockMovementDto dto);
    Task DeleteAsync(int id);
}
