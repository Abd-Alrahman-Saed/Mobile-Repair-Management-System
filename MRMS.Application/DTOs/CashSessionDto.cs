namespace MRMS.Application.DTOs;

public record CashSessionDto(
    int Id,
    DateTime BusinessDate,
    string OpenedByName,
    DateTime OpenedAt,
    decimal OpeningBalance,
    decimal CashSales,
    decimal RepairCollections,
    decimal CashExpenses,
    decimal ExpectedAmount,
    string? ClosedByName,
    DateTime? ClosedAt,
    decimal? CountedAmount,
    decimal? Difference,
    string Notes,
    bool IsClosed);
