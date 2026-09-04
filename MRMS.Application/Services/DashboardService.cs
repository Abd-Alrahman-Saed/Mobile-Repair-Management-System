using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;
    private readonly IRepairTicketService _repairTicketService;
    private readonly IIncomeStatementService _incomeStatementService;
    private readonly IPermissionService _permissions;

    public DashboardService(
        IUnitOfWork uow,
        IRepairTicketService repairTicketService,
        IIncomeStatementService incomeStatementService,
        IPermissionService permissions)
    {
        _uow = uow;
        _repairTicketService = repairTicketService;
        _incomeStatementService = incomeStatementService;
        _permissions = permissions;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(DateTime currentPeriodFrom, DateTime currentPeriodTo)
    {
        var repairStats = await _repairTicketService.GetDashboardStatsAsync() with
        {
            LowStockCount = await CountLowStockItemsAsync()
        };

        // The financial block (revenue, expenses, net profit, cash, stock value, payables) is
        // owner/manager-only. For everyone else return the operational counts only and leave the
        // money at zero - the dashboard hides that section for those roles.
        if (!_permissions.Can(Permission.ViewReports))
            return repairStats;

        var currentToEnd = currentPeriodTo.Date.AddDays(1).AddTicks(-1);
        var periodSpan = currentPeriodTo - currentPeriodFrom;
        var previousPeriodTo = currentPeriodFrom.AddTicks(-1);
        var previousPeriodFrom = previousPeriodTo - periodSpan;

        var (currentRevenue, previousRevenue) = await GetPeriodRevenuesAsync(currentPeriodFrom, currentToEnd, previousPeriodFrom, previousPeriodTo);
        var (currentExpenses, previousExpenses) = await GetPeriodExpensesAsync(currentPeriodFrom, currentToEnd, previousPeriodFrom, previousPeriodTo);
        var (currentNetProfit, previousNetProfit) = await GetPeriodNetProfitsAsync(currentPeriodFrom, currentToEnd, previousPeriodFrom, previousPeriodTo);

        var cashBalance = await CalculateCashBalanceAsync();
        var stockValue = await CalculateStockValueAsync();
        var payablesTotal = await CalculatePayablesTotalAsync();
        var overduePayablesCount = await CountOverduePayablesAsync();

        return repairStats with
        {
            CurrentPeriodRevenue = currentRevenue,
            PreviousPeriodRevenue = previousRevenue,
            RevenueChangePercent = CalculateChangePercent(currentRevenue, previousRevenue),
            CurrentPeriodExpenses = currentExpenses,
            PreviousPeriodExpenses = previousExpenses,
            ExpensesChangePercent = CalculateChangePercent(currentExpenses, previousExpenses),
            CurrentPeriodNetProfit = currentNetProfit,
            PreviousPeriodNetProfit = previousNetProfit,
            NetProfitChangePercent = CalculateChangePercent(currentNetProfit, previousNetProfit),
            CashBalance = cashBalance,
            StockValue = stockValue,
            PayablesTotal = payablesTotal,
            OverduePayablesCount = overduePayablesCount
        };
    }

    private async Task<(decimal current, decimal previous)> GetPeriodRevenuesAsync(DateTime curFrom, DateTime curTo, DateTime prevFrom, DateTime prevTo)
    {
        var sales = await _uow.Sales.GetAllAsync();
        var tickets = await _uow.RepairTickets.Query()
            .Where(t => t.Status == TicketStatus.Delivered && t.DeliveryDate != null)
            .ToListAsync();

        decimal Revenue(DateTime from, DateTime to)
        {
            var s = sales.Where(x => x.Date >= from && x.Date <= to).Sum(x => x.TotalPrice);
            var t = tickets.Where(x => x.DeliveryDate >= from && x.DeliveryDate <= to).Sum(x => x.Cost);
            return s + t;
        }

        return (Revenue(curFrom, curTo), Revenue(prevFrom, prevTo));
    }

    private async Task<(decimal current, decimal previous)> GetPeriodExpensesAsync(DateTime curFrom, DateTime curTo, DateTime prevFrom, DateTime prevTo)
    {
        var expenses = await _uow.Expenses.GetAllAsync();
        decimal Expenses(DateTime from, DateTime to) =>
            expenses.Where(e => e.Date >= from && e.Date <= to).Sum(e => e.Amount);
        return (Expenses(curFrom, curTo), Expenses(prevFrom, prevTo));
    }

    private async Task<(decimal current, decimal previous)> GetPeriodNetProfitsAsync(DateTime curFrom, DateTime curTo, DateTime prevFrom, DateTime prevTo)
    {
        // Net profit is an owner/manager-only figure (it needs the income statement, which requires
        // ViewReports). For roles that can't see it, leave the dashboard's financial KPIs at zero -
        // Phase 2.4 will null them out at the DTO instead.
        if (!_permissions.Can(Permission.ViewReports))
            return (0, 0);

        var current = await _incomeStatementService.GetIncomeStatementAsync(curFrom, curTo);
        var previous = await _incomeStatementService.GetIncomeStatementAsync(prevFrom, prevTo);
        return (current.NetProfit, previous.NetProfit);
    }

    private static double CalculateChangePercent(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return (double)((current - previous) / previous * 100);
    }

    private async Task<decimal> CalculateCashBalanceAsync()
    {
        var allSales = await _uow.Sales.GetAllAsync();
        var allTickets = await _uow.RepairTickets.Query()
            .Where(t => t.Status == TicketStatus.Delivered).ToListAsync();
        var allPurchases = await _uow.Purchases.GetAllAsync();
        var allExpenses = await _uow.Expenses.GetAllAsync();
        var allPayments = await _uow.SupplierPayments.GetAllAsync();

        // ticket.Cost is the full amount the customer pays (parts + labor). Repair parts cost is not a
        // separate cash inflow - it's a non-cash COGS reallocation (the parts were already paid for
        // via their Purchase). Adding it here would double-count cash that was never actually received twice.
        var inflows = allSales.Sum(s => s.TotalPrice)
                    + allTickets.Sum(t => t.Cost);

        var outflows = allPurchases.Sum(p => p.PaidAmount)
                     + allExpenses.Sum(e => e.Amount)
                     + allPayments.Sum(p => p.Amount);

        return inflows - outflows;
    }

    private async Task<decimal> CalculateStockValueAsync()
    {
        var items = await _uow.Items.GetAllAsync();
        return items.Sum(i => i.CurrentStock * i.CostPrice);
    }

    private async Task<decimal> CalculatePayablesTotalAsync()
    {
        var suppliers = await _uow.Suppliers.Query()
            .Include(s => s.Purchases)
            .Include(s => s.SupplierPayments)
            .ToListAsync();

        return suppliers.Sum(s =>
        {
            var purchases = s.Purchases.Sum(p => p.PurchaseItems.Sum(pi => pi.TotalCost));
            var payments = s.SupplierPayments.Sum(sp => sp.Amount);
            return Math.Max(0, s.OpeningBalance + purchases - payments);
        });
    }

    private async Task<int> CountLowStockItemsAsync()
    {
        var items = await _uow.Items.GetAllAsync();
        return items.Count(i => i.CurrentStock <= i.ReorderLevel);
    }

    private async Task<int> CountOverduePayablesAsync()
    {
        var suppliers = await _uow.Suppliers.Query()
            .Include(s => s.Purchases)
            .Include(s => s.SupplierPayments)
            .ToListAsync();

        return suppliers.Count(s =>
        {
            var purchases = s.Purchases.Sum(p => p.PurchaseItems.Sum(pi => pi.TotalCost));
            var payments = s.SupplierPayments.Sum(sp => sp.Amount);
            return s.OpeningBalance + purchases - payments > 0;
        });
    }
}
