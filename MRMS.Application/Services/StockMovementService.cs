using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class StockMovementService : IStockMovementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;

    public StockMovementService(IUnitOfWork unitOfWork, IJournalService journalService)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
    }

    public async Task<IEnumerable<StockMovementDto>> GetAllAsync()
    {
        var movements = await _unitOfWork.StockMovements
            .Query()
            .Include(m => m.Item)
            .OrderByDescending(m => m.Date)
            .ToListAsync();

        return movements.Select(MapToDto);
    }

    public async Task<IEnumerable<StockMovementDto>> GetByItemIdAsync(int itemId)
    {
        var movements = await _unitOfWork.StockMovements
            .Query()
            .Include(m => m.Item)
            .Where(m => m.ItemId == itemId)
            .OrderByDescending(m => m.Date)
            .ToListAsync();

        return movements.Select(MapToDto);
    }

    public async Task<StockMovementDto> CreateAsync(CreateStockMovementDto dto)
    {
        var movement = new StockMovement
        {
            Date = dto.Date,
            Type = dto.Type,
            ItemId = dto.ItemId,
            Quantity = dto.Quantity,
            Value = dto.Value,
            ReferenceId = dto.ReferenceId,
            Notes = dto.Notes
        };

        var item = await _unitOfWork.Items.GetByIdAsync(dto.ItemId);
        if (item is not null)
        {
            if (dto.Type == MovementType.CustomerReturn)
                item.CurrentStock += dto.Quantity;
            else
                item.CurrentStock -= dto.Quantity;
        }

        await _unitOfWork.StockMovements.AddAsync(movement);
        await _unitOfWork.CompleteAsync();

        var saved = await _unitOfWork.StockMovements
            .Query()
            .Include(m => m.Item)
            .FirstAsync(m => m.Id == movement.Id);

        if (dto.Type == MovementType.Damaged)
        {
            await _journalService.PostDamagedAsync(saved.Date, saved.Item.Name, saved.Quantity, saved.Value, saved.Id);
            await _unitOfWork.CompleteAsync();
        }

        return MapToDto(saved);
    }

    public async Task DeleteAsync(int id)
    {
        var movement = await _unitOfWork.StockMovements
            .Query()
            .Include(m => m.Item)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movement is null) return;

        if (movement.Item is not null)
        {
            if (movement.Type == MovementType.CustomerReturn)
                movement.Item.CurrentStock -= movement.Quantity;
            else
                movement.Item.CurrentStock += movement.Quantity;
        }

        _unitOfWork.StockMovements.Delete(movement);
        await _unitOfWork.CompleteAsync();
    }

    private static StockMovementDto MapToDto(StockMovement m) => new(
        m.Id, m.Date, m.Type, m.ItemId, m.Item.Name, m.Quantity, m.Value, m.ReferenceId, m.Notes, m.CreatedAt);
}
