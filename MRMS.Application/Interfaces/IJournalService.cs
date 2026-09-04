using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IJournalService
{
    Task PostSaleAsync(DateTime date, string description, decimal amount, int saleId);
    Task PostPurchaseAsync(DateTime date, string description, decimal amount, int purchaseId);
    Task PostExpenseAsync(DateTime date, string category, string description, decimal amount, int expenseId);
    /// <param name="totalCost">The full amount charged to the customer (parts + labor combined) - not labor alone.</param>
    /// <param name="partsCost">Cost basis of the parts used, posted separately as COGS against inventory.</param>
    Task PostRepairDeliveryAsync(DateTime date, string ticketNumber, decimal totalCost, decimal partsCost, int ticketId);
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
