using MRMS.Core.Enums;

namespace MRMS.Core.Entities;

public class StockMovement
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public MovementType Type { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public decimal Value { get; set; }
    public int? ReferenceId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Item Item { get; set; } = null!;
}
