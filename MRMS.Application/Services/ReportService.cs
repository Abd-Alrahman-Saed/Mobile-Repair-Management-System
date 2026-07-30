using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<StockBalanceItemDto>> GetStockBalanceAsync()
    {
        var items = await _uow.Items.GetAllAsync();
        return items.Select(i => new StockBalanceItemDto(
            i.Id, i.Code, i.Name, i.CurrentStock, i.CostPrice,
            i.CurrentStock * i.CostPrice, i.ReorderLevel,
            i.CurrentStock <= i.ReorderLevel));
    }

    public async Task<CashbookReportDto> GetCashbookAsync(DateTime from, DateTime to)
    {
        var entries = new List<CashbookEntryDto>();

        var sales = await _uow.Sales.Query()
            .Include(s => s.Item)
            .Where(s => s.Date >= from && s.Date <= to)
            .ToListAsync();
        foreach (var s in sales)
            entries.Add(new CashbookEntryDto(s.Date, $"بيع: {s.Item.Name} x{s.Quantity}", "وارد", s.TotalPrice, 0));

        var tickets = await _uow.RepairTickets.Query()
            .Where(t => t.Status == TicketStatus.Delivered && t.DeliveryDate >= from && t.DeliveryDate <= to)
            .ToListAsync();
        foreach (var t in tickets)
            entries.Add(new CashbookEntryDto(t.DeliveryDate!.Value, $"إصلاح: {t.TicketNumber}", "وارد", t.Cost, 0));

        var purchases = await _uow.Purchases.Query()
            .Include(p => p.Supplier)
            .Where(p => p.Date >= from && p.Date <= to)
            .ToListAsync();
        foreach (var p in purchases)
            entries.Add(new CashbookEntryDto(p.Date, $"مشتريات: {p.Supplier.Name}", "منصرف", p.PaidAmount, 0));

        var expenses = await _uow.Expenses.Query()
            .Where(e => e.Date >= from && e.Date <= to)
            .ToListAsync();
        foreach (var e in expenses)
            entries.Add(new CashbookEntryDto(e.Date, $"مصروف: {e.Category} - {e.Description}", "منصرف", e.Amount, 0));

        var supplierPayments = await _uow.SupplierPayments.Query()
            .Include(sp => sp.Supplier)
            .Where(sp => sp.Date >= from && sp.Date <= to)
            .ToListAsync();
        foreach (var sp in supplierPayments)
            entries.Add(new CashbookEntryDto(sp.Date, $"دفعة مورد: {sp.Supplier.Name}", "منصرف", sp.Amount, 0));

        var ordered = entries.OrderBy(e => e.Date).ToList();

        var openingBalance = await CalculateOpeningCashBalanceAsync(from);
        decimal running = openingBalance;
        var result = new List<CashbookEntryDto>();
        foreach (var entry in ordered)
        {
            running += entry.Type == "وارد" ? entry.Amount : -entry.Amount;
            result.Add(entry with { RunningBalance = running });
        }

        var totalInflows = ordered.Where(e => e.Type == "وارد").Sum(e => e.Amount);
        var totalOutflows = ordered.Where(e => e.Type == "منصرف").Sum(e => e.Amount);

        return new CashbookReportDto(openingBalance, totalInflows, totalOutflows, running, result);
    }

    public async Task<IEnumerable<PayableItemDto>> GetPayablesAsync()
    {
        var suppliers = await _uow.Suppliers
            .Query()
            .Include(s => s.Purchases)
            .Include(s => s.SupplierPayments)
            .ToListAsync();

        return suppliers.Select(s =>
        {
            var totalPurchases = s.Purchases.Sum(p => p.PurchaseItems.Sum(pi => pi.TotalCost));
            var totalPayments = s.SupplierPayments.Sum(sp => sp.Amount);
            var outstanding = s.OpeningBalance + totalPurchases - totalPayments;

            return new PayableItemDto(s.Id, s.Name, s.OpeningBalance, totalPurchases, totalPayments, outstanding);
        });
    }

    private async Task<decimal> CalculateOpeningCashBalanceAsync(DateTime from)
    {
        var allSales = await _uow.Sales.GetAllAsync();
        var allTickets = await _uow.RepairTickets.GetAllAsync();
        var allPurchases = await _uow.Purchases.GetAllAsync();
        var allExpenses = await _uow.Expenses.GetAllAsync();
        var allPayments = await _uow.SupplierPayments.GetAllAsync();

        var inflows = allSales.Where(s => s.Date < from).Sum(s => s.TotalPrice)
                    + allTickets.Where(t => t.Status == TicketStatus.Delivered && t.DeliveryDate < from).Sum(t => t.Cost);

        var outflows = allPurchases.Where(p => p.Date < from).Sum(p => p.PaidAmount)
                     + allExpenses.Where(e => e.Date < from).Sum(e => e.Amount)
                     + allPayments.Where(p => p.Date < from).Sum(p => p.Amount);

        return inflows - outflows;
    }
}
