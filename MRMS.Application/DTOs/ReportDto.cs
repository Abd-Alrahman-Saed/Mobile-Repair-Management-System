namespace MRMS.Application.DTOs;

public record StockBalanceItemDto(
    int ItemId,
    string Code,
    string Name,
    int CurrentStock,
    decimal CostPrice,
    decimal StockValue,
    int ReorderLevel,
    bool IsLowStock);

public record CashbookEntryDto(
    DateTime Date,
    string Description,
    string Type,
    decimal Amount,
    decimal RunningBalance,
    DateTime CreatedAt);

public record CashbookReportDto(
    decimal OpeningBalance,
    decimal TotalInflows,
    decimal TotalOutflows,
    decimal ClosingBalance,
    List<CashbookEntryDto> Entries);

public record PayableItemDto(
    int SupplierId,
    string SupplierName,
    decimal OpeningBalance,
    decimal TotalPurchases,
    decimal TotalPayments,
    decimal OutstandingBalance);
