using MRMS.Core.Enums;

namespace MRMS.Application.DTOs;

public record PurchaseDto(
    int Id,
    DateTime Date,
    int SupplierId,
    string SupplierName,
    PaymentMethod PaymentMethod,
    decimal PaidAmount,
    decimal TotalCost,
    string Notes,
    DateTime CreatedAt,
    List<PurchaseItemDto> Items,
    bool IsVoided = false,
    string? VoidReason = null);

public record PurchaseItemDto(
    int Id,
    int ItemId,
    string ItemName,
    int Quantity,
    decimal UnitCost,
    decimal TotalCost);

public record CreatePurchaseDto(
    DateTime Date,
    int SupplierId,
    PaymentMethod PaymentMethod,
    decimal PaidAmount,
    string Notes,
    List<CreatePurchaseItemDto> Items);

public record CreatePurchaseItemDto(
    int ItemId,
    int Quantity,
    decimal UnitCost);

public record UpdatePurchaseDto(
    int Id,
    DateTime Date,
    int SupplierId,
    PaymentMethod PaymentMethod,
    decimal PaidAmount,
    string Notes);
