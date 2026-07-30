using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IExpenseService
{
    Task<IEnumerable<ExpenseDto>> GetAllAsync();
    Task<ExpenseDto?> GetByIdAsync(int id);
    Task<ExpenseDto> CreateAsync(CreateExpenseDto dto);
    Task UpdateAsync(UpdateExpenseDto dto);
    Task DeleteAsync(int id);
}
