using MRMS.Application.DTOs;

namespace MRMS.Application.Interfaces;

public interface IReportService
{
    Task<IEnumerable<StockBalanceItemDto>> GetStockBalanceAsync();
    Task<CashbookReportDto> GetCashbookAsync(DateTime from, DateTime to);
    Task<IEnumerable<PayableItemDto>> GetPayablesAsync();
}
