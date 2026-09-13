namespace MRMS.Application.DTOs;

public record CustomerPaymentDto(
    int Id,
    DateTime Date,
    int CustomerId,
    string CustomerName,
    decimal Amount,
    string Reference,
    string Notes,
    DateTime CreatedAt,
    bool IsVoided = false,
    string? VoidReason = null);

public record CreateCustomerPaymentDto(
    DateTime Date,
    int CustomerId,
    decimal Amount,
    string Reference,
    string Notes);
