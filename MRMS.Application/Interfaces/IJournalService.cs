using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IJournalService
{
    Task PostSaleAsync(DateTime date, string description, decimal amount, int saleId);
    Task PostPurchaseAsync(DateTime date, string description, decimal amount, int purchaseId);
    Task PostExpenseAsync(DateTime date, string category, string description, decimal amount, int expenseId);
    Task PostRepairDeliveryAsync(DateTime date, string ticketNumber, decimal cost, int ticketId);
    Task PostSupplierPaymentAsync(DateTime date, string supplierName, decimal amount, int paymentId);
    Task PostDamagedAsync(DateTime date, string itemName, int quantity, decimal value, int movementId);
    Task<IEnumerable<JournalEntryDto>> GetAllEntriesAsync();
    Task<JournalEntryDto?> GetEntryByIdAsync(int id);
    Task<TrialBalanceDto> GetTrialBalanceAsync(DateTime? asOf = null);
}
