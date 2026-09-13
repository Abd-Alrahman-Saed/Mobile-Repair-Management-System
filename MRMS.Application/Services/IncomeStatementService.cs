using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class IncomeStatementService : IIncomeStatementService
{
    private readonly IUnitOfWork _uow;
    private readonly IPermissionService _permissions;

    public IncomeStatementService(IUnitOfWork uow, IPermissionService permissions)
    {
        _uow = uow;
        _permissions = permissions;
    }

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(DateTime from, DateTime to)
    {
        _permissions.Require(Permission.ViewReports);
        var toEnd = to.Date.AddDays(1).AddTicks(-1);

        var invoices = await _uow.SalesInvoices.Query()
            .Include(i => i.Lines)
            .Where(i => i.Date >= from && i.Date <= toEnd)
            .ToListAsync();

        var deliveredTickets = await _uow.RepairTickets.Query()
            .Where(t => t.Status == TicketStatus.Delivered && t.DeliveryDate >= from && t.DeliveryDate <= toEnd)
            .ToListAsync();

        var ticketIds = deliveredTickets.Select(t => t.Id).ToList();
        var repairParts = await _uow.RepairTicketItems.Query()
            .Where(rti => ticketIds.Contains(rti.RepairTicketId))
            .ToListAsync();

        var expenses = await _uow.Expenses.Query()
            .Where(e => e.Date >= from && e.Date <= toEnd)
            .ToListAsync();

        var damages = await _uow.StockMovements.Query()
            .Where(sm => sm.Type == MovementType.Damaged && sm.Date >= from && sm.Date <= toEnd)
            .ToListAsync();

        // Revenue is recognized accrual-basis (at the invoice/delivery date), regardless of how much
        // cash has actually been collected - the cashbook/dashboard track cash-basis separately.
        var salesRevenue = invoices.Sum(i => i.Total);
        var repairPartsCost = repairParts.Sum(rp => rp.TotalCost);
        // ticket.Cost is the final amount the customer pays (parts + labor combined, entered by the technician) —
        // it must NOT have repairPartsCost added on top, or revenue gets inflated by the parts cost.
        var repairRevenue = deliveredTickets.Sum(t => t.Cost);
        var totalRevenue = salesRevenue + repairRevenue;

        var cogs = invoices.Sum(i => i.Lines.Sum(l => l.Quantity * l.UnitCost));

        var grossProfit = totalRevenue - cogs - repairPartsCost;

        var operatingExpenses = expenses.Sum(e => e.Amount);
        var damageLosses = damages.Sum(d => d.Value);

        var netProfit = grossProfit - operatingExpenses - damageLosses;

        return new IncomeStatementDto(
            from, to,
            salesRevenue, repairRevenue, totalRevenue,
            cogs, repairPartsCost, grossProfit,
            operatingExpenses, damageLosses, netProfit);
    }
}
