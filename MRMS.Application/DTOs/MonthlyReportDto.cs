namespace MRMS.Application.DTOs;

public record MonthlyReportDto(
    int Year,
    int SelectedMonth,
    List<MonthlyRowDto> Rows,
    List<TopSellingItemDto> TopSellingItems,
    decimal CurrentMonthRevenue,
    decimal PreviousMonthRevenue,
    double RevenueChangePercent,
    decimal CurrentMonthExpenses,
    decimal PreviousMonthExpenses,
    double ExpensesChangePercent,
    decimal CurrentMonthNetProfit,
    decimal PreviousMonthNetProfit,
    double NetProfitChangePercent,
    decimal CurrentMonthCashBalance,
    decimal PreviousMonthCashBalance,
    double CashBalanceChangePercent);

public record MonthlyRowDto(
    int Month,
    string MonthName,
    decimal SalesRevenue,
    decimal RepairRevenue,
    decimal TotalRevenue,
    decimal CostOfGoodsSold,
    decimal PartsCost,
    decimal GrossProfit,
    decimal Expenses,
    decimal NetProfit,
    decimal PurchasesAmount,
    decimal SupplierPayments,
    decimal NetCashFlow,
    decimal CumulativeCashBalance);

public record TopSellingItemDto(
    string ItemName,
    int TotalQuantity,
    decimal TotalRevenue);
