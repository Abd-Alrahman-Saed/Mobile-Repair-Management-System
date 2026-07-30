namespace MRMS.Core.Entities;

public class Sale
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    public decimal CostPrice { get; set; }
    public decimal Profit => TotalPrice - (Quantity * CostPrice);
    public string CustomerName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Item Item { get; set; } = null!;
}
