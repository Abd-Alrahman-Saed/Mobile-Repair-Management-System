using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class IncomeStatementService : IIncomeStatementService
{
    private readonly IUnitOfWork _uow;

    public IncomeStatementService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(DateTime from, DateTime to)
    {
        var toEnd = to.Date.AddDays(1).AddTicks(-1);

        var sales = await _uow.Sales.Query()
            .Where(s => s.Date >= from && s.Date <= toEnd)
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

        var salesRevenue = sales.Sum(s => s.TotalPrice);
        var repairRevenue = deliveredTickets.Sum(t => t.Cost);
        var totalRevenue = salesRevenue + repairRevenue;

        var cogs = sales.Sum(s => s.Quantity * s.CostPrice);
        var repairPartsCost = repairParts.Sum(rp => rp.TotalCost);

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
