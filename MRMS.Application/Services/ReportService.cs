using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;
    private readonly IPermissionService _permissions;

    public ReportService(IUnitOfWork uow, IPermissionService permissions)
    {
        _uow = uow;
        _permissions = permissions;
    }

    public async Task<IEnumerable<StockBalanceItemDto>> GetStockBalanceAsync()
    {
        // Inventory screen - available to everyone. (The cost column itself is hidden per-DTO in 2.4.)
        var items = await _uow.Items.GetAllAsync();
        return items.Select(i => new StockBalanceItemDto(
            i.Id, i.Code, i.Name, i.CurrentStock, i.CostPrice,
            i.CurrentStock * i.CostPrice, i.ReorderLevel,
            i.CurrentStock <= i.ReorderLevel));
    }

    public async Task<CashbookReportDto> GetCashbookAsync(DateTime from, DateTime to)
    {
        _permissions.Require(Permission.ViewReports);
        var entries = new List<CashbookEntryDto>();

        var sales = await _uow.Sales.Query()
            .Include(s => s.Item)
            .Where(s => s.Date >= from && s.Date <= to)
            .ToListAsync();
        foreach (var s in sales)
            entries.Add(new CashbookEntryDto(s.Date, $"بيع: {s.Item.Name} x{s.Quantity}", "وارد", s.TotalPrice, 0, s.CreatedAt));

        // Repair cash comes from the actual payments collected on tickets (deposits + partials + final),
        // not the ticket's full Cost at delivery - so a deposit taken this week and the balance next week
        // each land in their own period. Repair parts cost is not separate cash received; it was already
        // paid for via the parts' Purchase.
        var repairPayments = await _uow.RepairTicketPayments.Query()
            .Include(p => p.RepairTicket).ThenInclude(t => t.Device)
            .Where(p => p.BusinessDate >= from && p.BusinessDate <= to)
            .ToListAsync();
        foreach (var p in repairPayments)
        {
            var deviceName = $"{p.RepairTicket.Device?.Brand} {p.RepairTicket.Device?.Model}".Trim();
            entries.Add(new CashbookEntryDto(
                p.BusinessDate, $"صيانة: {deviceName}", "وارد", p.Amount, 0, p.CollectedAt));
        }

        var purchases = await _uow.Purchases.Query()
            .Include(p => p.Supplier)
            .Where(p => p.Date >= from && p.Date <= to)
            .ToListAsync();
        foreach (var p in purchases)
            entries.Add(new CashbookEntryDto(p.Date, $"مشتريات: {p.Supplier.Name}", "منصرف", p.PaidAmount, 0, p.CreatedAt));

        var expenses = await _uow.Expenses.Query()
            .Where(e => e.Date >= from && e.Date <= to)
            .ToListAsync();
        foreach (var e in expenses)
            entries.Add(new CashbookEntryDto(e.Date, $"مصروف: {e.Category} - {e.Description}", "منصرف", e.Amount, 0, e.CreatedAt));

        var supplierPayments = await _uow.SupplierPayments.Query()
            .Include(sp => sp.Supplier)
            .Where(sp => sp.Date >= from && sp.Date <= to)
            .ToListAsync();
        foreach (var sp in supplierPayments)
            entries.Add(new CashbookEntryDto(sp.Date, $"دفعة مورد: {sp.Supplier.Name}", "منصرف", sp.Amount, 0, sp.CreatedAt));

        // Order chronologically (oldest first) so the running balance accumulates correctly; within the
        // same calendar day, break ties by actual entry time so same-day order reflects reality, not
        // just which category was queried first. The final list is reversed below (newest first), so
        // this also makes the most-recently-entered transaction land at the very top, as requested.
        var chronological = entries.OrderBy(e => e.Date).ThenBy(e => e.CreatedAt).ToList();

        var openingBalance = await CalculateOpeningCashBalanceAsync(from);
        decimal running = openingBalance;
        var result = new List<CashbookEntryDto>();
        foreach (var entry in chronological)
        {
            running += entry.Type == "وارد" ? entry.Amount : -entry.Amount;
            result.Add(entry with { RunningBalance = running });
        }
        result.Reverse();

        var totalInflows = chronological.Where(e => e.Type == "وارد").Sum(e => e.Amount);
        var totalOutflows = chronological.Where(e => e.Type == "منصرف").Sum(e => e.Amount);

        return new CashbookReportDto(openingBalance, totalInflows, totalOutflows, running, result);
    }

    public async Task<IEnumerable<PayableItemDto>> GetPayablesAsync()
    {
        // Soft hide, not a hard block: the suppliers screen is open to everyone, but the money columns
        // are for ViewSupplierBalance only. The payables *report* screen is gated by navigation.
        var showFigures = _permissions.Can(Permission.ViewSupplierBalance);

        var suppliers = await _uow.Suppliers
            .Query()
            .Include(s => s.Purchases)
            .Include(s => s.SupplierPayments)
            .ToListAsync();

        return suppliers.Select(s =>
        {
            if (!showFigures)
                return new PayableItemDto(s.Id, s.Name, null, null, null, null);

            var totalPurchases = s.Purchases.Sum(p => p.PurchaseItems.Sum(pi => pi.TotalCost));
            var totalPayments = s.SupplierPayments.Sum(sp => sp.Amount);
            var outstanding = s.OpeningBalance + totalPurchases - totalPayments;

            return new PayableItemDto(s.Id, s.Name, s.OpeningBalance, totalPurchases, totalPayments, outstanding);
        });
    }

    private async Task<decimal> CalculateOpeningCashBalanceAsync(DateTime from)
    {
        var allSales = await _uow.Sales.GetAllAsync();
        var allRepairPayments = await _uow.RepairTicketPayments.GetAllAsync();
        var allPurchases = await _uow.Purchases.GetAllAsync();
        var allExpenses = await _uow.Expenses.GetAllAsync();
        var allPayments = await _uow.SupplierPayments.GetAllAsync();

        // Repair cash is the sum of payments actually collected before the window, not delivered tickets'
        // Cost. Repair parts cost is not an additional cash inflow (paid for via the parts' Purchase).
        var inflows = allSales.Where(s => s.Date < from).Sum(s => s.TotalPrice)
                    + allRepairPayments.Where(p => p.BusinessDate < from).Sum(p => p.Amount);

        var outflows = allPurchases.Where(p => p.Date < from).Sum(p => p.PaidAmount)
                     + allExpenses.Where(e => e.Date < from).Sum(e => e.Amount)
                     + allPayments.Where(p => p.Date < from).Sum(p => p.Amount);

        return inflows - outflows;
    }
}
