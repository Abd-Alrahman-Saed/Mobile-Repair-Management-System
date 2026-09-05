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

    public bool IsVoided { get; set; }
    public int? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }
    public DateTime? VoidedAt { get; set; }

    /// <summary>The cash session open when this sale was recorded - null if none was open.</summary>
    public int? CashSessionId { get; set; }

    public Item Item { get; set; } = null!;
}
