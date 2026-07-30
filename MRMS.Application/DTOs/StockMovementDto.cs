using MRMS.Core.Enums;

namespace MRMS.Application.DTOs;

public record StockMovementDto(
    int Id,
    DateTime Date,
    MovementType Type,
    int ItemId,
    string ItemName,
    int Quantity,
    decimal Value,
    int? ReferenceId,
    string Notes,
    DateTime CreatedAt);

public record CreateStockMovementDto(
    DateTime Date,
    MovementType Type,
    int ItemId,
    int Quantity,
    decimal Value,
    int? ReferenceId,
    string Notes);
