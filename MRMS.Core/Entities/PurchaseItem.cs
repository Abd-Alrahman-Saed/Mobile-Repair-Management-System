namespace MRMS.Core.Entities;

public class PurchaseItem
{
    public int Id { get; set; }
    public int PurchaseId { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost => Quantity * UnitCost;

    public Purchase Purchase { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
