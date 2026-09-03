using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class MonthlyReportService : IMonthlyReportService
{
    private readonly IUnitOfWork _uow;
    private readonly IPermissionService _permissions;
    private static readonly string[] ArabicMonths =
        ["يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
         "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"];

    public MonthlyReportService(IUnitOfWork uow, IPermissionService permissions)
    {
        _uow = uow;
        _permissions = permissions;
    }

    public async Task<MonthlyReportDto> GetMonthlyReportAsync(int year, int month)
    {
        _permissions.Require(Permission.ViewReports);
        var allSales = await _uow.Sales.Query()
            .Where(s => s.Date.Year == year).ToListAsync();

        var allDeliveredTickets = await _uow.RepairTickets.Query()
            .Where(t => t.Status == TicketStatus.Delivered && t.DeliveryDate!.Value.Year == year)
            .ToListAsync();

        var deliveredIds = allDeliveredTickets.Select(t => t.Id).ToList();
        var allParts = await _uow.RepairTicketItems.Query()
            .Where(rti => deliveredIds.Contains(rti.RepairTicketId))
            .ToListAsync();

        var allExpenses = await _uow.Expenses.Query()
            .Where(e => e.Date.Year == year).ToListAsync();

        var allPurchases = await _uow.Purchases.Query()
            .Where(p => p.Date.Year == year).ToListAsync();

        var allPayments = await _uow.SupplierPayments.Query()
            .Where(sp => sp.Date.Year == year).ToListAsync();

        var rows = new List<MonthlyRowDto>();
        decimal runningCash = 0;

        for (int m = 1; m <= 12; m++)
        {
            var sales = allSales.Where(s => s.Date.Month == m).ToList();
            var tickets = allDeliveredTickets.Where(t => t.DeliveryDate!.Value.Month == m).ToList();
            var ticketIds = tickets.Select(t => t.Id).ToHashSet();
            var parts = allParts.Where(p => ticketIds.Contains(p.RepairTicketId)).ToList();
            var expenses = allExpenses.Where(e => e.Date.Month == m).ToList();
            var purchases = allPurchases.Where(p => p.Date.Month == m).ToList();
            var payments = allPayments.Where(sp => sp.Date.Month == m).ToList();

            var salesRevenue = sales.Sum(s => s.TotalPrice);
            var partsCost = parts.Sum(p => p.TotalCost);
            // ticket.Cost already includes parts + labor (the full amount the customer pays) - don't add partsCost again.
            var repairRevenue = tickets.Sum(t => t.Cost);
            var totalRevenue = salesRevenue + repairRevenue;
            var cogs = sales.Sum(s => s.Quantity * s.CostPrice);
            var grossProfit = totalRevenue - cogs - partsCost;
            var expSum = expenses.Sum(e => e.Amount);
            var netProfit = grossProfit - expSum;
            var purchasesAmount = purchases.Sum(p => p.PaidAmount);
            var paymentsAmount = payments.Sum(p => p.Amount);

            var cashIn = salesRevenue + repairRevenue;
            var cashOut = expSum + purchasesAmount + paymentsAmount;
            var netCashFlow = cashIn - cashOut;
            runningCash += netCashFlow;

            rows.Add(new MonthlyRowDto(
                m, ArabicMonths[m - 1],
                salesRevenue, repairRevenue, totalRevenue,
                cogs, partsCost, grossProfit,
                expSum, netProfit,
                purchasesAmount, paymentsAmount,
                netCashFlow, runningCash));
        }

        var currentMonthRow = rows[month - 1];
        var previousMonthRow = month > 1 ? rows[month - 2] : null;

        var topItems = await GetTopSellingItemsAsync(year);

        return new MonthlyReportDto(
            year, month, rows, topItems,
            currentMonthRow.TotalRevenue,
            previousMonthRow?.TotalRevenue ?? 0,
            CalcChange(currentMonthRow.TotalRevenue, previousMonthRow?.TotalRevenue ?? 0),
            currentMonthRow.Expenses,
            previousMonthRow?.Expenses ?? 0,
            CalcChange(currentMonthRow.Expenses, previousMonthRow?.Expenses ?? 0),
            currentMonthRow.NetProfit,
            previousMonthRow?.NetProfit ?? 0,
            CalcChange(currentMonthRow.NetProfit, previousMonthRow?.NetProfit ?? 0),
            currentMonthRow.CumulativeCashBalance,
            previousMonthRow?.CumulativeCashBalance ?? 0,
            CalcChange(currentMonthRow.CumulativeCashBalance, previousMonthRow?.CumulativeCashBalance ?? 0));
    }

    private async Task<List<TopSellingItemDto>> GetTopSellingItemsAsync(int year)
    {
        var sales = await _uow.Sales.Query()
            .Include(s => s.Item)
            .Where(s => s.Date.Year == year)
            .ToListAsync();

        return sales
            .GroupBy(s => s.Item.Name)
            .Select(g => new TopSellingItemDto(g.Key, g.Sum(s => s.Quantity), g.Sum(s => s.TotalPrice)))
            .OrderByDescending(x => x.TotalRevenue)
            .Take(10)
            .ToList();
    }

    private static double CalcChange(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return (double)((current - previous) / previous * 100);
    }
}
