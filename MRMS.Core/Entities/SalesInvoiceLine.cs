namespace MRMS.Core.Entities;

public class SalesInvoiceLine
{
    public int Id { get; set; }
    public int SalesInvoiceId { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    /// <summary>Locked from <c>Item.CostPrice</c> at the moment of sale - never taken from the
    /// caller, so a receptionist's form (which can't see cost) can't distort the profit calculation.</summary>
    public decimal UnitCost { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;

    public SalesInvoice SalesInvoice { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
