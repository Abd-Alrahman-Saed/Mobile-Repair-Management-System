using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IJournalService
{
    Task PostSaleAsync(DateTime date, string description, decimal amount, int saleId);
    Task PostPurchaseAsync(DateTime date, string description, decimal amount, int purchaseId);
    Task PostExpenseAsync(DateTime date, string category, string description, decimal amount, int expenseId);
    /// <summary>Posts cash-basis revenue for a single payment (deposit or partial/final) actually
    /// collected on a repair ticket - called at the moment of collection, not at delivery, so revenue
    /// is only ever recognized for cash actually received.</summary>
    Task PostRepairPaymentAsync(DateTime date, string ticketNumber, decimal amount, int ticketId);
    /// <summary>Posts the COGS entry for parts consumed on a repair ticket. Called at delivery -
    /// parts consumption is independent of payment timing.</summary>
    Task PostRepairPartsCostAsync(DateTime date, string ticketNumber, decimal partsCost, int ticketId);
    Task PostSupplierPaymentAsync(DateTime date, string supplierName, decimal amount, int paymentId);
    Task PostDamagedAsync(DateTime date, string itemName, int quantity, decimal value, int movementId);

    /// <summary>
    /// Posts a mirror (debit/credit swapped) of every journal entry that references
    /// (<paramref name="referenceType"/>, <paramref name="referenceId"/>), so a voided transaction nets
    /// to zero in the ledger without deleting the original entries.
    /// </summary>
    Task PostReversalAsync(string referenceType, int referenceId, DateTime date, string reason);
    Task<IEnumerable<JournalEntryDto>> GetAllEntriesAsync();
    Task<JournalEntryDto?> GetEntryByIdAsync(int id);
    Task<TrialBalanceDto> GetTrialBalanceAsync(DateTime? asOf = null);
}
