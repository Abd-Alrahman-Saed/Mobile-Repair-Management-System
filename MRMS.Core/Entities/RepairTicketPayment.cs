namespace MRMS.Core.Entities;

/// <summary>One payment collected against a repair ticket (a deposit at intake, a partial payment,
/// or the final settlement). The sum of these equals <see cref="RepairTicket.PaidAmount"/>. Each row
/// is attributed to the cash session that was open when it was collected, so a ticket paid across
/// two shifts is split correctly between them.</summary>
public class RepairTicketPayment
{
    public int Id { get; set; }
    public int RepairTicketId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>The business day this collection belongs to (local <c>DateTime.Today</c> at collection),
    /// matching the <c>Date</c> convention on <see cref="Sale"/>/<see cref="Expense"/> and
    /// <see cref="CashSession.BusinessDate"/>. Used to bucket the payment in cash reports.</summary>
    public DateTime BusinessDate { get; set; }

    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public int CollectedByUserId { get; set; }
    public string CollectedByName { get; set; } = string.Empty;

    /// <summary>The open cash session at collection time - null if none was open.</summary>
    public int? CashSessionId { get; set; }

    public RepairTicket RepairTicket { get; set; } = null!;
}
