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

    public SupplierPaymentService(IUnitOfWork unitOfWork, IJournalService journalService,
        IAuditService audit, IPermissionService permissions)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
        _audit = audit;
        _permissions = permissions;
    }

    public async Task<IEnumerable<SupplierPaymentDto>> GetAllAsync()
    {
        _permissions.Require(Permission.ManageSuppliers);
        var payments = await _unitOfWork.SupplierPayments
            .Query()
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

    public async Task DeleteAsync(int id)
    {
        _permissions.Require(Permission.ManageSuppliers);
        var payment = await _unitOfWork.SupplierPayments.GetByIdAsync(id);
        if (payment is null) return;

        _unitOfWork.SupplierPayments.Delete(payment);
        await _unitOfWork.CompleteAsync();
    }

    private static SupplierPaymentDto MapToDto(SupplierPayment p) => new(
        p.Id, p.Date, p.SupplierId, p.Supplier.Name, p.Amount, p.Reference, p.Notes, p.CreatedAt);
}
