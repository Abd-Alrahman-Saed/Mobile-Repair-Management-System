using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class SaleService : ISaleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;
    private readonly IAuditService _audit;
    private readonly IPermissionService _permissions;

    public SaleService(IUnitOfWork unitOfWork, IJournalService journalService,
        IAuditService audit, IPermissionService permissions)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
        _audit = audit;
        _permissions = permissions;
    }

    public async Task<IEnumerable<SaleDto>> GetAllAsync()
    {
        var sales = await _unitOfWork.Sales
            .Query()
            .Include(s => s.Item)
            .OrderByDescending(s => s.Date).ThenByDescending(s => s.CreatedAt)
            .ToListAsync();

        return sales.Select(MapToDto);
    }

    public async Task<SaleDto?> GetByIdAsync(int id)
    {
        var sale = await _unitOfWork.Sales
            .Query()
            .Include(s => s.Item)
            .FirstOrDefaultAsync(s => s.Id == id);

        return sale is null ? null : MapToDto(sale);
    }

    public async Task<SaleDto> CreateAsync(CreateSaleDto dto)
    {
        if (dto.Quantity <= 0)
            throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر");

        if (dto.UnitPrice <= 0)
            throw new InvalidOperationException("السعر يجب أن يكون أكبر من صفر");

        var item = await _unitOfWork.Items.GetByIdAsync(dto.ItemId)
            ?? throw new InvalidOperationException("الصنف غير موجود");

        var sale = new Sale
        {
            Date = dto.Date,
            ItemId = dto.ItemId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            // Cost basis is locked from the item at the moment of sale - never taken from the caller,
            // so a receptionist's form (which can't see cost) can't zero out the profit calculation.
            CostPrice = item.CostPrice,
            CustomerName = dto.CustomerName,
            Notes = dto.Notes
        };

        if (item.CurrentStock < dto.Quantity)
            throw new InvalidOperationException($"الرصيد غير كافٍ. المتوفر: {item.CurrentStock}، المطلوب: {dto.Quantity}");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            item.CurrentStock -= dto.Quantity;

            await _unitOfWork.Sales.AddAsync(sale);
            await _unitOfWork.CompleteAsync();

            await _journalService.PostSaleAsync(sale.Date, $"بيع: {item.Name} x{sale.Quantity} - {sale.CustomerName}", sale.TotalPrice, sale.Id);
            await _audit.LogAsync("Sale.Create", nameof(Sale), sale.Id,
                $"{item.Name} x{sale.Quantity} = {sale.TotalPrice:N0}");
            await _unitOfWork.CompleteAsync();
        });

        return (await GetByIdAsync(sale.Id))!;
    }

    public async Task UpdateAsync(UpdateSaleDto dto)
    {
        var existing = await _unitOfWork.Sales.GetByIdAsync(dto.Id);
        if (existing is null) return;

        var oldQty = existing.Quantity;
        var diff = dto.Quantity - oldQty;

        if (dto.Quantity <= 0)
            throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر");

        if (dto.UnitPrice <= 0)
            throw new InvalidOperationException("السعر يجب أن يكون أكبر من صفر");

        existing.Date = dto.Date;
        existing.ItemId = dto.ItemId;
        existing.Quantity = dto.Quantity;
        existing.UnitPrice = dto.UnitPrice;
        existing.CustomerName = dto.CustomerName;
        existing.Notes = dto.Notes;

        var item = await _unitOfWork.Items.GetByIdAsync(dto.ItemId);
        if (item is not null && diff > 0 && item.CurrentStock < diff)
            throw new InvalidOperationException($"الرصيد غير كافٍ. المتوفر: {item.CurrentStock}");

        if (item is not null)
            item.CurrentStock -= diff;

        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var sale = await _unitOfWork.Sales.GetByIdAsync(id);
        if (sale is null) return;

        var item = await _unitOfWork.Items.GetByIdAsync(sale.ItemId);
        if (item is not null)
            item.CurrentStock += sale.Quantity;

        _unitOfWork.Sales.Delete(sale);
        await _unitOfWork.CompleteAsync();
    }

    private SaleDto MapToDto(Sale s)
    {
        var canSeeProfit = _permissions.Can(Permission.ViewProfit);
        return new(
            s.Id, s.Date, s.ItemId, s.Item.Name,
            s.Quantity, s.UnitPrice, s.TotalPrice,
            canSeeProfit ? s.CostPrice : null,
            canSeeProfit ? s.Profit : null,
            s.CustomerName, s.Notes, s.CreatedAt);
    }
}
