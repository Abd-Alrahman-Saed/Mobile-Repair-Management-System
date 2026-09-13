namespace MRMS.Core.Entities;

/// <summary>A collection from a customer, independent of any specific invoice - mirrors
/// <see cref="SupplierPayment"/> on the sales side. Reduces the customer's overall outstanding
/// balance (see <c>IReportService.GetReceivablesAsync</c>), not a particular invoice's balance.</summary>
public class CustomerPayment
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsVoided { get; set; }
    public int? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }
    public DateTime? VoidedAt { get; set; }

    /// <summary>The cash session open when this payment was collected - null if none was open.</summary>
    public int? CashSessionId { get; set; }

    public Customer Customer { get; set; } = null!;
}
