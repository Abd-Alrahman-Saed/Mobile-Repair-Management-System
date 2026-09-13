using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IReportService
{
    Task<IEnumerable<StockBalanceItemDto>> GetStockBalanceAsync();
    Task<CashbookReportDto> GetCashbookAsync(DateTime from, DateTime to);
    Task<IEnumerable<PayableItemDto>> GetPayablesAsync();
    /// <summary>Every customer's outstanding balance (total invoiced minus total collected). Visible
    /// to everyone - no permission gate, unlike <see cref="GetPayablesAsync"/>.</summary>
    Task<IEnumerable<ReceivableItemDto>> GetReceivablesAsync();
}
