namespace MRMS.Application.DTOs;

public record SupplierPaymentDto(
    int Id,
    DateTime Date,
    int SupplierId,
    string SupplierName,
    decimal Amount,
    string Reference,
    string Notes,
    DateTime CreatedAt);

public record CreateSupplierPaymentDto(
    DateTime Date,
    int SupplierId,
    decimal Amount,
    string Reference,
    string Notes);
