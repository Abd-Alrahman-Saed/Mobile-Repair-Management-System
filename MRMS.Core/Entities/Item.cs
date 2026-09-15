using MRMS.Core.Enums;

namespace MRMS.Core.Entities;

public class Item
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ItemType Type { get; set; } = ItemType.Accessory;
    public string Unit { get; set; } = "قطعة";
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int ReorderLevel { get; set; }
    /// <summary>Null for <see cref="ItemType.Service"/> items - they carry no stock and are excluded
    /// from stock-value/low-stock calculations and never block a sale for insufficient stock.</summary>
    public int? CurrentStock { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseItem> PurchaseItems { get; set; } = [];
    public ICollection<SalesInvoiceLine> SalesInvoiceLines { get; set; } = [];
    public ICollection<RepairTicketItem> RepairTicketItems { get; set; } = [];
    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
