using MRMS.Core.Enums;

namespace MRMS.Core.Entities;

/// <summary>A multi-line sales invoice - replaces the old single-item <c>Sale</c>. Cash-basis:
/// <see cref="PaidAmount"/> is what was actually collected at the moment of sale (may be less than
/// <see cref="Total"/> for a credit sale); later collections are separate <see cref="CustomerPayment"/>
/// rows, not edits to this invoice.</summary>
public class SalesInvoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;

    public int? CustomerId { get; set; }
    /// <summary>Free-text name for a quick sale with no registered customer. Always populated (from
    /// the customer's name when <see cref="CustomerId"/> is set) so the invoice reads fine either way.</summary>
    public string CustomerName { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    /// <summary>Amount actually collected at the moment of sale - not necessarily <see cref="Total"/>.</summary>
    public decimal PaidAmount { get; set; }
    /// <summary>What's still owed by the customer on this invoice alone (later CustomerPayments are
    /// not tied to a specific invoice, so this doesn't reflect them).</summary>
    public decimal RemainingAmount => Total - PaidAmount;

    public int? CashSessionId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsVoided { get; set; }
    public int? VoidedByUserId { get; set; }
    public string? VoidReason { get; set; }
    public DateTime? VoidedAt { get; set; }

    public Customer? Customer { get; set; }
    public ICollection<SalesInvoiceLine> Lines { get; set; } = [];
}
