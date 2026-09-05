namespace MRMS.Core.Entities;

/// <summary>One cash-drawer shift: opened with a counted starting balance, closed with a counted
/// ending balance. Only one session may be open at a time (a single physical drawer). Once closed,
/// its <see cref="ExpectedAmount"/>/<see cref="CountedAmount"/>/<see cref="Difference"/> are frozen
/// and non-owners can no longer touch cash transactions dated on its <see cref="BusinessDate"/>.</summary>
public class CashSession
{
    public int Id { get; set; }

    /// <summary>The business day this shift belongs to (<c>OpenedAt.Date</c>). Used to lock same-day
    /// transactions after close.</summary>
    public DateTime BusinessDate { get; set; }

    public int OpenedByUserId { get; set; }
    public string OpenedByName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public decimal OpeningBalance { get; set; }

    public int? ClosedByUserId { get; set; }
    public string? ClosedByName { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>Opening + cash sales + repair collections - cash expenses, frozen at close time.</summary>
    public decimal? ExpectedAmount { get; set; }
    public decimal? CountedAmount { get; set; }
    /// <summary><c>CountedAmount - ExpectedAmount</c>: positive = surplus, negative = shortage.</summary>
    public decimal? Difference { get; set; }

    public string Notes { get; set; } = string.Empty;

    public bool IsClosed => ClosedAt is not null;
}
