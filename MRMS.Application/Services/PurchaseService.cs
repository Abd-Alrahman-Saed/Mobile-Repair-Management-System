using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;
    private readonly IAuditService _audit;

    public PurchaseService(IUnitOfWork unitOfWork, IJournalService journalService, IAuditService audit)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
        _audit = audit;
    }

    public async Task<IEnumerable<PurchaseDto>> GetAllAsync()
    {
        var purchases = await _unitOfWork.Purchases
            .Query()
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseItems).ThenInclude(pi => pi.Item)
            .OrderByDescending(p => p.Date).ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        return purchases.Select(MapToDto);
    }

    public async Task<PurchaseDto?> GetByIdAsync(int id)
    {
        var purchase = await _unitOfWork.Purchases
            .Query()
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseItems).ThenInclude(pi => pi.Item)
            .FirstOrDefaultAsync(p => p.Id == id);

        return purchase is null ? null : MapToDto(purchase);
    }

    public async Task<PurchaseDto> CreateAsync(CreatePurchaseDto dto)
    {
        var purchase = new Purchase
        {
            Date = dto.Date,
            SupplierId = dto.SupplierId,
            PaymentMethod = dto.PaymentMethod,
            PaidAmount = dto.PaidAmount,
            Notes = dto.Notes
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var itemDto in dto.Items)
            {
                var purchaseItem = new PurchaseItem
                {
                    ItemId = itemDto.ItemId,
                    Quantity = itemDto.Quantity,
                    UnitCost = itemDto.UnitCost
                };
                purchase.PurchaseItems.Add(purchaseItem);

                var item = await _unitOfWork.Items.GetByIdAsync(itemDto.ItemId);
                if (item is not null)
                    item.CurrentStock += itemDto.Quantity;
            }

            await _unitOfWork.Purchases.AddAsync(purchase);
            await _unitOfWork.CompleteAsync();

            var supplierName = (await _unitOfWork.Suppliers.GetByIdAsync(dto.SupplierId))?.Name ?? $"Supplier#{dto.SupplierId}";
            await _journalService.PostPurchaseAsync(purchase.Date, $"مشتريات: {supplierName} - {purchase.PaidAmount:N2}", purchase.PaidAmount, purchase.Id);
            await _audit.LogAsync("Purchase.Create", nameof(Purchase), purchase.Id,
                $"{supplierName} - مدفوع {purchase.PaidAmount:N0}");
            await _unitOfWork.CompleteAsync();
        });

        return (await GetByIdAsync(purchase.Id))!;
    }

    public async Task UpdateAsync(UpdatePurchaseDto dto)
    {
        var purchase = await _unitOfWork.Purchases.GetByIdAsync(dto.Id);
        if (purchase is null) return;

        purchase.Date = dto.Date;
        purchase.SupplierId = dto.SupplierId;
        purchase.PaymentMethod = dto.PaymentMethod;
        purchase.PaidAmount = dto.PaidAmount;
        purchase.Notes = dto.Notes;

        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var purchase = await _unitOfWork.Purchases
            .Query()
            .Include(p => p.PurchaseItems)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchase is null) return;

        foreach (var item in purchase.PurchaseItems)
        {
            var stockItem = await _unitOfWork.Items.GetByIdAsync(item.ItemId);
            if (stockItem is not null)
                stockItem.CurrentStock -= item.Quantity;
        }

        _unitOfWork.Purchases.Delete(purchase);
        await _unitOfWork.CompleteAsync();
    }

    private static PurchaseDto MapToDto(Purchase p) => new(
        p.Id, p.Date, p.SupplierId, p.Supplier.Name,
        p.PaymentMethod, p.PaidAmount,
        p.PurchaseItems.Sum(pi => pi.TotalCost),
        p.Notes, p.CreatedAt,
        p.PurchaseItems.Select(pi => new PurchaseItemDto(
            pi.Id, pi.ItemId, pi.Item.Name, pi.Quantity, pi.UnitCost, pi.TotalCost)).ToList());
}
