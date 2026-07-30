using MRMS.Core.Enums;

namespace MRMS.Application.DTOs;

public record ItemDto(
    int Id,
    string Code,
    string Name,
    ItemType Type,
    string Unit,
    decimal CostPrice,
    decimal SellingPrice,
    int ReorderLevel,
    int CurrentStock,
    DateTime CreatedAt);

public record CreateItemDto(
    string Code,
    string Name,
    ItemType Type,
    string Unit,
    decimal CostPrice,
    decimal SellingPrice,
    int ReorderLevel);

public record UpdateItemDto(
    int Id,
    string Code,
    string Name,
    ItemType Type,
    string Unit,
    decimal CostPrice,
    decimal SellingPrice,
    int ReorderLevel);
