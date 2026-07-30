using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class SupplierPaymentService : ISupplierPaymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupplierPaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SupplierPaymentDto>> GetAllAsync()
    {
        var payments = await _unitOfWork.SupplierPayments
            .Query()
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        return payments.Select(MapToDto);
    }

    public async Task<IEnumerable<SupplierPaymentDto>> GetBySupplierIdAsync(int supplierId)
    {
        var payments = await _unitOfWork.SupplierPayments
            .Query()
            .Include(p => p.Supplier)
            .Where(p => p.SupplierId == supplierId)
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        return payments.Select(MapToDto);
    }

    public async Task<SupplierPaymentDto> CreateAsync(CreateSupplierPaymentDto dto)
    {
        var payment = new SupplierPayment
        {
            Date = dto.Date,
            SupplierId = dto.SupplierId,
            Amount = dto.Amount,
            Reference = dto.Reference,
            Notes = dto.Notes
        };

        await _unitOfWork.SupplierPayments.AddAsync(payment);
        await _unitOfWork.CompleteAsync();

        var saved = await _unitOfWork.SupplierPayments
            .Query()
            .Include(p => p.Supplier)
            .FirstAsync(p => p.Id == payment.Id);

        return MapToDto(saved);
    }

    public async Task DeleteAsync(int id)
    {
        var payment = await _unitOfWork.SupplierPayments.GetByIdAsync(id);
        if (payment is null) return;

        _unitOfWork.SupplierPayments.Delete(payment);
        await _unitOfWork.CompleteAsync();
    }

    private static SupplierPaymentDto MapToDto(SupplierPayment p) => new(
        p.Id, p.Date, p.SupplierId, p.Supplier.Name, p.Amount, p.Reference, p.Notes, p.CreatedAt);
}
