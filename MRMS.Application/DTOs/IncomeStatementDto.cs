namespace MRMS.Application.DTOs;

public record IncomeStatementDto(
    DateTime From,
    DateTime To,
    decimal SalesRevenue,
    decimal RepairRevenue,
    decimal TotalRevenue,
    decimal CostOfGoodsSold,
    decimal RepairPartsCost,
    decimal GrossProfit,
    decimal OperatingExpenses,
    decimal DamageLosses,
    decimal NetProfit);
