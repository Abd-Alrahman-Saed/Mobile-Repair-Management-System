using MRMS.Core.Enums;

namespace MRMS.Core.Entities;

public class Purchase
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public int SupplierId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public decimal PaidAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsVoided { get; set; }
    public int? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }
    public DateTime? VoidedAt { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseItem> PurchaseItems { get; set; } = [];
}
