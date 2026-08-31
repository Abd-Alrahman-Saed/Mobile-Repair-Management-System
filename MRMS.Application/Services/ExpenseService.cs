using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJournalService _journalService;

    public ExpenseService(IUnitOfWork unitOfWork, IJournalService journalService)
    {
        _unitOfWork = unitOfWork;
        _journalService = journalService;
    }

    public async Task<IEnumerable<ExpenseDto>> GetAllAsync()
    {
        var expenses = await _unitOfWork.Expenses
            .Query()
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

        await _unitOfWork.Expenses.AddAsync(expense);
        await _unitOfWork.CompleteAsync();

        await _journalService.PostExpenseAsync(expense.Date, expense.Category, expense.Description, expense.Amount, expense.Id);
        await _unitOfWork.CompleteAsync();

        return MapToDto(expense);
    }

    public async Task UpdateAsync(UpdateExpenseDto dto)
    {
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

    public async Task DeleteAsync(int id)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(id);
        if (expense is null) return;

        _unitOfWork.Expenses.Delete(expense);
        await _unitOfWork.CompleteAsync();
    }

    private static ExpenseDto MapToDto(Expense e) => new(
        e.Id, e.Date, e.Category, e.Description, e.Amount,
        e.PaymentMethod, e.Notes, e.CreatedAt);
}
