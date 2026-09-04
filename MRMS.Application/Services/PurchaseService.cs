using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;
    private readonly IAuditService _audit;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUserService _currentUser;

    public PurchaseService(IUnitOfWork unitOfWork, IJournalService journalService, IAuditService audit,
        IPermissionService permissions, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
        _audit = audit;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<PurchaseDto>> GetAllAsync()
    {
        var query = _unitOfWork.Purchases.Query();
        if (_permissions.Can(Permission.VoidTransaction))
            query = query.IgnoreQueryFilters();

        var purchases = await query
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

    public async Task VoidAsync(int id, string reason)
    {
        _permissions.Require(Permission.VoidTransaction);
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("سبب الإلغاء مطلوب");

        var purchase = await _unitOfWork.Purchases.Query()
            .IgnoreQueryFilters()
            .Include(p => p.PurchaseItems)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("فاتورة المشتريات غير موجودة");
        if (purchase.IsVoided)
            throw new InvalidOperationException("الفاتورة ملغاة بالفعل");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            purchase.IsVoided = true;
            purchase.VoidedByUserId = _currentUser.User?.Id;
            purchase.VoidReason = reason.Trim();
            purchase.VoidedAt = DateTime.UtcNow;

            foreach (var line in purchase.PurchaseItems)
            {
                var stockItem = await _unitOfWork.Items.GetByIdAsync(line.ItemId);
                if (stockItem is not null)
                    stockItem.CurrentStock -= line.Quantity;
            }

            await _journalService.PostReversalAsync(nameof(Purchase), purchase.Id, DateTime.UtcNow.Date, reason.Trim());
            await _audit.LogAsync("Purchase.Void", nameof(Purchase), purchase.Id, reason.Trim());
            await _unitOfWork.CompleteAsync();
        });
    }

    private static PurchaseDto MapToDto(Purchase p) => new(
        p.Id, p.Date, p.SupplierId, p.Supplier.Name,
        p.PaymentMethod, p.PaidAmount,
        p.PurchaseItems.Sum(pi => pi.TotalCost),
        p.Notes, p.CreatedAt,
        p.PurchaseItems.Select(pi => new PurchaseItemDto(
            pi.Id, pi.ItemId, pi.Item.Name, pi.Quantity, pi.UnitCost, pi.TotalCost)).ToList(),
        p.IsVoided, p.VoidReason);
}
