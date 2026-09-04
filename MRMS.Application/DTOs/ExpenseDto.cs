using MRMS.Core.Enums;

namespace MRMS.Application.DTOs;

public record ExpenseDto(
    int Id,
    DateTime Date,
    string Category,
    string Description,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string Notes,
    DateTime CreatedAt,
    bool IsVoided = false,
    string? VoidReason = null);

public record CreateExpenseDto(
    DateTime Date,
    string Category,
    string Description,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string Notes);

public record UpdateExpenseDto(
    int Id,
    DateTime Date,
    string Category,
    string Description,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string Notes);
