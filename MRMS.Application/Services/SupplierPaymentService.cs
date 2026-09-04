using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class SupplierPaymentService : ISupplierPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;
    private readonly IAuditService _audit;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUserService _currentUser;

    public SupplierPaymentService(IUnitOfWork unitOfWork, IJournalService journalService,
        IAuditService audit, IPermissionService permissions, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
        _audit = audit;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<SupplierPaymentDto>> GetAllAsync()
    {
        _permissions.Require(Permission.ManageSuppliers);
        var query = _unitOfWork.SupplierPayments.Query();
        if (_permissions.Can(Permission.VoidTransaction))
            query = query.IgnoreQueryFilters();

        var payments = await query
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.Date).ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        return payments.Select(MapToDto);
    }

    public async Task<IEnumerable<SupplierPaymentDto>> GetBySupplierIdAsync(int supplierId)
    {
        _permissions.Require(Permission.ManageSuppliers);
        var payments = await _unitOfWork.SupplierPayments
            .Query()
            .Include(p => p.Supplier)
            .Where(p => p.SupplierId == supplierId)
            .OrderByDescending(p => p.Date).ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        return payments.Select(MapToDto);
    }

    public async Task<SupplierPaymentDto> CreateAsync(CreateSupplierPaymentDto dto)
    {
        _permissions.Require(Permission.ManageSuppliers);
        var payment = new SupplierPayment
        {
            Date = dto.Date,
            SupplierId = dto.SupplierId,
            Amount = dto.Amount,
            Reference = dto.Reference,
            Notes = dto.Notes
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.SupplierPayments.AddAsync(payment);
            await _unitOfWork.CompleteAsync();

            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(payment.SupplierId);
            var supplierName = supplier?.Name ?? $"Supplier#{payment.SupplierId}";
            await _journalService.PostSupplierPaymentAsync(payment.Date, supplierName, payment.Amount, payment.Id);
            await _audit.LogAsync("SupplierPayment.Create", nameof(SupplierPayment), payment.Id,
                $"{supplierName} - {payment.Amount:N0}");
            await _unitOfWork.CompleteAsync();
        });

        var saved = await _unitOfWork.SupplierPayments
            .Query()
            .Include(p => p.Supplier)
            .FirstAsync(p => p.Id == payment.Id);

        return MapToDto(saved);
    }

    public async Task VoidAsync(int id, string reason)
    {
        _permissions.Require(Permission.VoidTransaction);
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("سبب الإلغاء مطلوب");

        var payment = await _unitOfWork.SupplierPayments.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("الدفعة غير موجودة");
        if (payment.IsVoided)
            throw new InvalidOperationException("الدفعة ملغاة بالفعل");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            payment.IsVoided = true;
            payment.VoidedByUserId = _currentUser.User?.Id;
            payment.VoidReason = reason.Trim();
            payment.VoidedAt = DateTime.UtcNow;

            await _journalService.PostReversalAsync(nameof(SupplierPayment), payment.Id, DateTime.UtcNow.Date, reason.Trim());
            await _audit.LogAsync("SupplierPayment.Void", nameof(SupplierPayment), payment.Id, reason.Trim());
            await _unitOfWork.CompleteAsync();
        });
    }

    private static SupplierPaymentDto MapToDto(SupplierPayment p) => new(
        p.Id, p.Date, p.SupplierId, p.Supplier.Name, p.Amount, p.Reference, p.Notes, p.CreatedAt,
        p.IsVoided, p.VoidReason);
}
