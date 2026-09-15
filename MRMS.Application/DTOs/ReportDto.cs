namespace MRMS.Application.DTOs;

public record StockBalanceItemDto(
    int ItemId,
    string Code,
    string Name,
    // Null for a Service item (no stock tracked). CostPrice/StockValue are null when the current
    // user lacks Permission.ViewCostPrice.
    int? CurrentStock,
    decimal? CostPrice,
    decimal? StockValue,
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
    // Financial figures are null when the current user lacks Permission.ViewSupplierBalance.
    decimal? OpeningBalance,
    decimal? TotalPurchases,
    decimal? TotalPayments,
    decimal? OutstandingBalance);

// Unlike PayableItemDto, receivables are visible to everyone (no permission gate) - front-desk staff
// need to see a customer's debt before deciding whether to extend more credit.
public record ReceivableItemDto(
    int CustomerId,
    string CustomerName,
    decimal TotalInvoiced,
    decimal TotalCollected,
    decimal OutstandingBalance);
