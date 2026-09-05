using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Authorization;
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
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemSettingService _settings;
    private readonly ICashSessionService _cashSessions;

    public SaleService(IUnitOfWork unitOfWork, IJournalService journalService, IAuditService audit,
        IPermissionService permissions, ICurrentUserService currentUser, ISystemSettingService settings,
        ICashSessionService cashSessions)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
        _audit = audit;
        _permissions = permissions;
        _currentUser = currentUser;
        _settings = settings;
        _cashSessions = cashSessions;
    }

    public async Task<IEnumerable<SaleDto>> GetAllAsync()
    {
        var query = _unitOfWork.Sales.Query();
        // Only a user who can void transactions is shown the voided rows (struck through) - everyone
        // else, and every report, sees the global filter's already-clean list.
        if (_permissions.Can(Permission.VoidTransaction))
            query = query.IgnoreQueryFilters();

        var sales = await query
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

        await _cashSessions.EnsureNotLockedAsync(dto.Date);

        var item = await _unitOfWork.Items.GetByIdAsync(dto.ItemId)
            ?? throw new InvalidOperationException("الصنف غير موجود");

        var approver = await ResolvePricingApproverAsync(dto.ApprovedByUserId);
        await EnforcePricingRulesAsync(dto.UnitPrice, item, approver);

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
            Notes = dto.Notes,
            CashSessionId = await _cashSessions.GetCurrentSessionIdAsync()
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
            if (approver is not null)
                await _audit.LogAsync("Sale.OverrideApproved", nameof(Sale), sale.Id,
                    $"سعر {sale.UnitPrice:N0} بموافقة {approver.FullName}");
            await _unitOfWork.CompleteAsync();
        });

        return (await GetByIdAsync(sale.Id))!;
    }

    public async Task VoidAsync(int id, string reason)
    {
        _permissions.Require(Permission.VoidTransaction);
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("سبب الإلغاء مطلوب");

        var sale = await _unitOfWork.Sales.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new InvalidOperationException("عملية البيع غير موجودة");
        if (sale.IsVoided)
            throw new InvalidOperationException("العملية ملغاة بالفعل");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            sale.IsVoided = true;
            sale.VoidedByUserId = _currentUser.User?.Id;
            sale.VoidReason = reason.Trim();
            sale.VoidedAt = DateTime.UtcNow;

            var item = await _unitOfWork.Items.GetByIdAsync(sale.ItemId);
            if (item is not null)
                item.CurrentStock += sale.Quantity;

            await _journalService.PostReversalAsync(nameof(Sale), sale.Id, DateTime.UtcNow.Date, reason.Trim());
            await _audit.LogAsync("Sale.Void", nameof(Sale), sale.Id, reason.Trim());
            await _unitOfWork.CompleteAsync();
        });
    }

    private async Task<CurrentUser?> ResolvePricingApproverAsync(int? approverId)
    {
        if (approverId is not { } aid) return null;
        var user = await _unitOfWork.Users.GetByIdAsync(aid);
        if (user is null || !user.IsActive || !RolePermissions.Has(user.Role, Permission.ApplyDiscountBeyondLimit))
            return null;
        return new CurrentUser(user.Id, user.Username, user.FullName, user.Role);
    }

    private async Task EnforcePricingRulesAsync(decimal unitPrice, Item item, CurrentUser? approver)
    {
        // An owner (or a live owner/manager approval) overrides both checks.
        if (_permissions.Can(Permission.ApplyDiscountBeyondLimit) || approver is not null)
            return;

        if (unitPrice < item.CostPrice && !_permissions.Can(Permission.SellBelowCost))
            throw new InvalidOperationException("سعر البيع أقل من التكلفة ويتطلب موافقة المالك");

        if (item.SellingPrice > 0)
        {
            var discountPct = (item.SellingPrice - unitPrice) / item.SellingPrice * 100m;
            if (discountPct > await MaxDiscountPercentAsync())
                throw new InvalidOperationException("هذا الخصم يتجاوز الحد المسموح لك ويتطلب موافقة المالك");
        }
    }

    private async Task<decimal> MaxDiscountPercentAsync()
    {
        var key = _currentUser.Role switch
        {
            UserRole.Manager => "Discount.MaxPercent.Manager",
            UserRole.Receptionist => "Discount.MaxPercent.Receptionist",
            _ => null
        };
        if (key is null) return 0m;
        var setting = await _settings.GetByKeyAsync(key);
        return decimal.TryParse(setting?.Value, out var pct) ? pct : 0m;
    }

    public async Task UpdateAsync(UpdateSaleDto dto)
    {
        var existing = await _unitOfWork.Sales.GetByIdAsync(dto.Id);
        if (existing is null) return;

        await _cashSessions.EnsureNotLockedAsync(existing.Date);
        await _cashSessions.EnsureNotLockedAsync(dto.Date);

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

    private SaleDto MapToDto(Sale s)
    {
        var canSeeProfit = _permissions.Can(Permission.ViewProfit);
        return new(
            s.Id, s.Date, s.ItemId, s.Item.Name,
            s.Quantity, s.UnitPrice, s.TotalPrice,
            canSeeProfit ? s.CostPrice : null,
            canSeeProfit ? s.Profit : null,
            s.CustomerName, s.Notes, s.CreatedAt,
            s.IsVoided, s.VoidReason);
    }
}
