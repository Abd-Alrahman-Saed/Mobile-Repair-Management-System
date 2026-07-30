namespace MRMS.Core.Entities;

public class SupplierPayment
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public int SupplierId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Supplier Supplier { get; set; } = null!;
}
