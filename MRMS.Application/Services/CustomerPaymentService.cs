using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

/// <summary>Collections from a customer, independent of any specific invoice - mirrors
/// <see cref="SupplierPaymentService"/> on the purchasing side.</summary>
public class CustomerPaymentService : ICustomerPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;
    private readonly IAuditService _audit;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUserService _currentUser;
    private readonly ICashSessionService _cashSessions;

    public CustomerPaymentService(IUnitOfWork unitOfWork, IJournalService journalService,
        IAuditService audit, IPermissionService permissions, ICurrentUserService currentUser,
        ICashSessionService cashSessions)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
        _audit = audit;
        _permissions = permissions;
        _currentUser = currentUser;
        _cashSessions = cashSessions;
    }

    public async Task<IEnumerable<CustomerPaymentDto>> GetAllAsync()
    {
        var query = _unitOfWork.CustomerPayments.Query();
        if (_permissions.Can(Permission.VoidTransaction))
            query = query.IgnoreQueryFilters();

        var payments = await query
            .Include(p => p.Customer)
            .OrderByDescending(p => p.Date).ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        return payments.Select(MapToDto);
    }

    public async Task<IEnumerable<CustomerPaymentDto>> GetByCustomerIdAsync(int customerId)
    {
        var payments = await _unitOfWork.CustomerPayments
            .Query()
            .Include(p => p.Customer)
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.Date).ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        return payments.Select(MapToDto);
    }

    public async Task<CustomerPaymentDto> CreateAsync(CreateCustomerPaymentDto dto)
    {
        if (dto.Amount <= 0)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

        await _cashSessions.EnsureNotLockedAsync(dto.Date);

        var payment = new CustomerPayment
        {
            Date = dto.Date,
            CustomerId = dto.CustomerId,
            Amount = dto.Amount,
            Reference = dto.Reference,
            Notes = dto.Notes
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            payment.CashSessionId = await _cashSessions.GetCurrentSessionIdAsync();
            await _unitOfWork.CustomerPayments.AddAsync(payment);
            await _unitOfWork.CompleteAsync();

            var customer = await _unitOfWork.Customers.GetByIdAsync(payment.CustomerId);
            var customerName = customer?.FullName ?? $"Customer#{payment.CustomerId}";
            await _journalService.PostCustomerPaymentAsync(payment.Date, customerName, payment.Amount, payment.Id);
            await _audit.LogAsync("CustomerPayment.Create", nameof(CustomerPayment), payment.Id,
                $"{customerName} - {payment.Amount:N0}");
            await _unitOfWork.CompleteAsync();
        });

        var saved = await _unitOfWork.CustomerPayments
            .Query()
            .Include(p => p.Customer)
            .FirstAsync(p => p.Id == payment.Id);

        return MapToDto(saved);
    }

    public async Task VoidAsync(int id, string reason)
    {
        _permissions.Require(Permission.VoidTransaction);
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("سبب الإلغاء مطلوب");

        var payment = await _unitOfWork.CustomerPayments.Query()
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

            await _journalService.PostReversalAsync(nameof(CustomerPayment), payment.Id, DateTime.UtcNow.Date, reason.Trim());
            await _audit.LogAsync("CustomerPayment.Void", nameof(CustomerPayment), payment.Id, reason.Trim());
            await _unitOfWork.CompleteAsync();
        });
    }

    private static CustomerPaymentDto MapToDto(CustomerPayment p) => new(
        p.Id, p.Date, p.CustomerId, p.Customer.FullName, p.Amount, p.Reference, p.Notes, p.CreatedAt,
        p.IsVoided, p.VoidReason);
}
