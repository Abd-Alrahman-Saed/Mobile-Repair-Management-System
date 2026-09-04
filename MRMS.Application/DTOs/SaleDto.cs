namespace MRMS.Application.DTOs;

public record SaleDto(
    int Id,
    DateTime Date,
    int ItemId,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    // Null when the current user lacks Permission.ViewProfit.
    decimal? CostPrice,
    decimal? Profit,
    string CustomerName,
    string Notes,
    DateTime CreatedAt);

public record CreateSaleDto(
    DateTime Date,
    int ItemId,
    int Quantity,
    decimal UnitPrice,
    decimal CostPrice,
    string CustomerName,
    string Notes);

public record UpdateSaleDto(
    int Id,
    DateTime Date,
    int ItemId,
    int Quantity,
    decimal UnitPrice,
    string CustomerName,
    string Notes);
