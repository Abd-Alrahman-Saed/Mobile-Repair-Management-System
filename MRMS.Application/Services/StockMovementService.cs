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
    private readonly IAuditService _audit;

    public StockMovementService(IUnitOfWork unitOfWork, IJournalService journalService, IAuditService audit)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
        _audit = audit;
    }

    public async Task<IEnumerable<StockMovementDto>> GetAllAsync()
    {
        var movements = await _unitOfWork.StockMovements
            .Query()
            .Include(m => m.Item)
            .OrderByDescending(m => m.Date).ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        return movements.Select(MapToDto);
    }

    public async Task<IEnumerable<StockMovementDto>> GetByItemIdAsync(int itemId)
    {
        var movements = await _unitOfWork.StockMovements
            .Query()
            .Include(m => m.Item)
            .Where(m => m.ItemId == itemId)
            .OrderByDescending(m => m.Date).ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        return movements.Select(MapToDto);
    }

    public async Task<StockMovementDto> CreateAsync(CreateStockMovementDto dto)
    {
        if (dto.Quantity <= 0)
            throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر");

        var item = await _unitOfWork.Items.GetByIdAsync(dto.ItemId)
            ?? throw new InvalidOperationException("الصنف غير موجود");

        if (dto.Type != MovementType.CustomerReturn && item.CurrentStock < dto.Quantity)
            throw new InvalidOperationException($"الرصيد غير كافٍ. المتوفر: {item.CurrentStock}، المطلوب: {dto.Quantity}");

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

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (dto.Type == MovementType.CustomerReturn)
                item.CurrentStock += dto.Quantity;
            else
                item.CurrentStock -= dto.Quantity;

            await _unitOfWork.StockMovements.AddAsync(movement);
            await _unitOfWork.CompleteAsync();

            await _audit.LogAsync("StockMovement.Create", nameof(StockMovement), movement.Id,
                $"{dto.Type} - {item.Name} x{movement.Quantity}");

            if (dto.Type == MovementType.Damaged)
                await _journalService.PostDamagedAsync(movement.Date, item.Name, movement.Quantity, movement.Value, movement.Id);

            await _unitOfWork.CompleteAsync();
        });

        var saved = await _unitOfWork.StockMovements
            .Query()
            .Include(m => m.Item)
            .FirstAsync(m => m.Id == movement.Id);

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
