using MRMS.Core.Enums;

namespace MRMS.Application.DTOs;

public record SalesInvoiceLineDto(
    int Id,
    int ItemId,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    // Null when the current user lacks Permission.ViewProfit.
    decimal? UnitCost,
    decimal LineTotal);

public record SalesInvoiceDto(
    int Id,
    string InvoiceNumber,
    DateTime Date,
    int? CustomerId,
    string CustomerName,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    PaymentMethod PaymentMethod,
    decimal PaidAmount,
    string Notes,
    DateTime CreatedAt,
    List<SalesInvoiceLineDto> Lines,
    // Null when the current user lacks Permission.ViewProfit.
    decimal? Profit,
    bool IsVoided = false,
    string? VoidReason = null)
{
    public decimal RemainingAmount => Total - PaidAmount;
}

public record CreateSalesInvoiceLineDto(int ItemId, int Quantity, decimal UnitPrice);

public record CreateSalesInvoiceDto(
    DateTime Date,
    int? CustomerId,
    string CustomerName,
    PaymentMethod PaymentMethod,
    List<CreateSalesInvoiceLineDto> Lines,
    decimal DiscountAmount,
    decimal PaidAmount,
    string Notes,
    // Set when an over-limit discount or below-cost price was approved live by an owner/manager.
    int? ApprovedByUserId = null);
