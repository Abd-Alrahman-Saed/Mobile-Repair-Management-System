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
    DateTime CreatedAt,
    bool IsVoided = false,
    string? VoidReason = null);

public record CreateSaleDto(
    DateTime Date,
    int ItemId,
    int Quantity,
    decimal UnitPrice,
    decimal CostPrice,
    string CustomerName,
    string Notes,
    // Set when an over-limit discount or below-cost price was approved live by an owner/manager.
    int? ApprovedByUserId = null);

public record UpdateSaleDto(
    int Id,
    DateTime Date,
    int ItemId,
    int Quantity,
    decimal UnitPrice,
    string CustomerName,
    string Notes);
