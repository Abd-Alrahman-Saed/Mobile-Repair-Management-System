using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;
    private readonly IAuditService _audit;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUserService _currentUser;

    public ExpenseService(IUnitOfWork unitOfWork, IJournalService journalService,
        IAuditService audit, IPermissionService permissions, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
        _audit = audit;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<ExpenseDto>> GetAllAsync()
    {
        var query = _unitOfWork.Expenses.Query();
        if (_permissions.Can(Permission.VoidTransaction))
            query = query.IgnoreQueryFilters();

        var expenses = await query
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt)
            .ToListAsync();

        return expenses.Select(MapToDto);
    }

    public async Task<ExpenseDto?> GetByIdAsync(int id)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(id);
        return expense is null ? null : MapToDto(expense);
    }

    public async Task<ExpenseDto> CreateAsync(CreateExpenseDto dto)
    {
        _permissions.Require(Permission.ManageExpenses);
        if (dto.Amount <= 0)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

        if (string.IsNullOrWhiteSpace(dto.Category))
            throw new InvalidOperationException("التصنيف مطلوب");

        var expense = new Expense
        {
            Date = dto.Date,
            Category = dto.Category,
            Description = dto.Description,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            Notes = dto.Notes
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.Expenses.AddAsync(expense);
            await _unitOfWork.CompleteAsync();

            await _journalService.PostExpenseAsync(expense.Date, expense.Category, expense.Description, expense.Amount, expense.Id);
            await _audit.LogAsync("Expense.Create", nameof(Expense), expense.Id,
                $"{expense.Category} - {expense.Amount:N0}");
            await _unitOfWork.CompleteAsync();
        });

        return MapToDto(expense);
    }

    public async Task UpdateAsync(UpdateExpenseDto dto)
    {
        _permissions.Require(Permission.ManageExpenses);
        if (dto.Amount <= 0)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

        if (string.IsNullOrWhiteSpace(dto.Category))
            throw new InvalidOperationException("التصنيف مطلوب");

        var expense = await _unitOfWork.Expenses.GetByIdAsync(dto.Id);
        if (expense is null) return;

        expense.Date = dto.Date;
        expense.Category = dto.Category;
        expense.Description = dto.Description;
        expense.Amount = dto.Amount;
        expense.PaymentMethod = dto.PaymentMethod;
        expense.Notes = dto.Notes;

        await _unitOfWork.CompleteAsync();
    }

    public async Task VoidAsync(int id, string reason)
    {
        _permissions.Require(Permission.VoidTransaction);
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("سبب الإلغاء مطلوب");

        var expense = await _unitOfWork.Expenses.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException("المصروف غير موجود");
        if (expense.IsVoided)
            throw new InvalidOperationException("المصروف ملغى بالفعل");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            expense.IsVoided = true;
            expense.VoidedByUserId = _currentUser.User?.Id;
            expense.VoidReason = reason.Trim();
            expense.VoidedAt = DateTime.UtcNow;

            await _journalService.PostReversalAsync(nameof(Expense), expense.Id, DateTime.UtcNow.Date, reason.Trim());
            await _audit.LogAsync("Expense.Void", nameof(Expense), expense.Id, reason.Trim());
            await _unitOfWork.CompleteAsync();
        });
    }

    private static ExpenseDto MapToDto(Expense e) => new(
        e.Id, e.Date, e.Category, e.Description, e.Amount,
        e.PaymentMethod, e.Notes, e.CreatedAt, e.IsVoided, e.VoidReason);
}
