using MRMS.Core.Enums;

namespace MRMS.Application.DTOs;

public class ItemDto
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public ItemType Type { get; init; }
    public string Unit { get; init; } = "";
    /// <summary>Null when the current user lacks <c>Permission.ViewCostPrice</c> (hidden at the DTO,
    /// not just the column).</summary>
    public decimal? CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int ReorderLevel { get; init; }
    public int CurrentStock { get; set; }
    public DateTime CreatedAt { get; init; }
}

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
