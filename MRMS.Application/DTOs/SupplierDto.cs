namespace MRMS.Application.DTOs;

public record SupplierDto(
    int Id,
    string Code,
    string Name,
    string Phone,
    decimal OpeningBalance,
    string Notes,
    DateTime CreatedAt);

public record CreateSupplierDto(
    string Code,
    string Name,
    string Phone,
    decimal OpeningBalance,
    string Notes);

public record UpdateSupplierDto(
    int Id,
    string Code,
    string Name,
    string Phone,
    decimal OpeningBalance,
    string Notes);
