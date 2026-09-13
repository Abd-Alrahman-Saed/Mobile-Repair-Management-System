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

    public async Task<IEnumerable<SalesInvoiceDto>> GetAllAsync()
    {
        var query = _unitOfWork.SalesInvoices.Query();
        if (_permissions.Can(Permission.VoidTransaction))
            query = query.IgnoreQueryFilters();

        var invoices = await query
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Customer)
            .OrderByDescending(i => i.Date).ThenByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invoices.Select(MapToDto);
    }

    public async Task<SalesInvoiceDto?> GetByIdAsync(int id)
    {
        var invoice = await _unitOfWork.SalesInvoices.Query()
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id);

        return invoice is null ? null : MapToDto(invoice);
    }

    public async Task<IEnumerable<SalesInvoiceDto>> GetByCustomerIdAsync(int customerId)
    {
        var invoices = await _unitOfWork.SalesInvoices.Query()
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Customer)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.Date).ThenByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invoices.Select(MapToDto);
    }

    public async Task<SalesInvoiceDto> CreateAsync(CreateSalesInvoiceDto dto)
    {
        if (dto.Lines is not { Count: > 0 })
            throw new InvalidOperationException("يجب إضافة صنف واحد على الأقل للفاتورة");

        if (dto.PaymentMethod == PaymentMethod.Credit && dto.CustomerId is null)
            throw new InvalidOperationException("البيع الآجل يتطلب اختيار عميل مسجّل");

        await _cashSessions.EnsureNotLockedAsync(dto.Date);

        var approver = await ResolvePricingApproverAsync(dto.ApprovedByUserId);

        var invoice = new SalesInvoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            Date = dto.Date,
            CustomerId = dto.CustomerId,
            PaymentMethod = dto.PaymentMethod,
            Notes = dto.Notes
        };

        var subtotal = 0m;
        foreach (var lineDto in dto.Lines)
        {
            if (lineDto.Quantity <= 0)
                throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر");
            if (lineDto.UnitPrice <= 0)
                throw new InvalidOperationException("السعر يجب أن يكون أكبر من صفر");

            var item = await _unitOfWork.Items.GetByIdAsync(lineDto.ItemId)
                ?? throw new InvalidOperationException("الصنف غير موجود");
            if (item.CurrentStock < lineDto.Quantity)
                throw new InvalidOperationException($"الرصيد غير كافٍ للصنف {item.Name}. المتوفر: {item.CurrentStock}");

            await EnforcePricingRulesAsync(lineDto.UnitPrice, item, approver);

            invoice.Lines.Add(new SalesInvoiceLine
            {
                ItemId = lineDto.ItemId,
                Quantity = lineDto.Quantity,
                UnitPrice = lineDto.UnitPrice,
                // Cost basis is locked from the item at the moment of sale - never taken from the
                // caller, so a receptionist's form (which can't see cost) can't zero out profit.
                UnitCost = item.CostPrice
            });
            subtotal += lineDto.Quantity * lineDto.UnitPrice;
        }

        invoice.Subtotal = subtotal;
        invoice.DiscountAmount = dto.DiscountAmount;
        invoice.Total = subtotal - dto.DiscountAmount;
        invoice.PaidAmount = dto.PaidAmount;

        var customer = dto.CustomerId is { } cid ? await _unitOfWork.Customers.GetByIdAsync(cid) : null;
        invoice.CustomerName = customer?.FullName ?? dto.CustomerName;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var line in invoice.Lines)
            {
                var item = await _unitOfWork.Items.GetByIdAsync(line.ItemId);
                if (item is not null)
                    item.CurrentStock -= line.Quantity;
            }

            invoice.CashSessionId = await _cashSessions.GetCurrentSessionIdAsync();
            await _unitOfWork.SalesInvoices.AddAsync(invoice);
            await _unitOfWork.CompleteAsync();

            if (invoice.PaidAmount > 0)
                await _journalService.PostSalesInvoicePaymentAsync(invoice.Date, invoice.InvoiceNumber, invoice.PaidAmount, invoice.Id);

            await _audit.LogAsync("SalesInvoice.Create", nameof(SalesInvoice), invoice.Id,
                $"{invoice.InvoiceNumber} - {invoice.Total:N0}");
            if (approver is not null)
                await _audit.LogAsync("SalesInvoice.OverrideApproved", nameof(SalesInvoice), invoice.Id,
                    $"بموافقة {approver.FullName}");
            await _unitOfWork.CompleteAsync();
        });

        return (await GetByIdAsync(invoice.Id))!;
    }

    public async Task VoidAsync(int id, string reason)
    {
        _permissions.Require(Permission.VoidTransaction);
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("سبب الإلغاء مطلوب");

        var invoice = await _unitOfWork.SalesInvoices.Query()
            .IgnoreQueryFilters()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException("الفاتورة غير موجودة");
        if (invoice.IsVoided)
            throw new InvalidOperationException("الفاتورة ملغاة بالفعل");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            invoice.IsVoided = true;
            invoice.VoidedByUserId = _currentUser.User?.Id;
            invoice.VoidReason = reason.Trim();
            invoice.VoidedAt = DateTime.UtcNow;

            foreach (var line in invoice.Lines)
            {
                var item = await _unitOfWork.Items.GetByIdAsync(line.ItemId);
                if (item is not null)
                    item.CurrentStock += line.Quantity;
            }

            await _journalService.PostReversalAsync(nameof(SalesInvoice), invoice.Id, DateTime.UtcNow.Date, reason.Trim());
            await _audit.LogAsync("SalesInvoice.Void", nameof(SalesInvoice), invoice.Id, reason.Trim());
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

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var count = (await _unitOfWork.SalesInvoices.Query().IgnoreQueryFilters().CountAsync()) + 1;
        return $"INV-{count:D5}";
    }

    private SalesInvoiceDto MapToDto(SalesInvoice i)
    {
        var canSeeProfit = _permissions.Can(Permission.ViewProfit);
        var lines = i.Lines.Select(l => new SalesInvoiceLineDto(
            l.Id, l.ItemId, l.Item.Name, l.Quantity, l.UnitPrice,
            canSeeProfit ? l.UnitCost : null, l.LineTotal)).ToList();

        decimal? profit = canSeeProfit
            ? i.Lines.Sum(l => l.LineTotal) - i.Lines.Sum(l => l.Quantity * l.UnitCost) - i.DiscountAmount
            : null;

        return new SalesInvoiceDto(
            i.Id, i.InvoiceNumber, i.Date, i.CustomerId,
            i.Customer?.FullName ?? i.CustomerName,
            i.Subtotal, i.DiscountAmount, i.Total, i.PaymentMethod, i.PaidAmount,
            i.Notes, i.CreatedAt, lines, profit, i.IsVoided, i.VoidReason);
    }
}
