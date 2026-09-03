using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class JournalService : IJournalService
{
    private readonly IUnitOfWork _uow;
    private readonly IPermissionService _permissions;

    public JournalService(IUnitOfWork uow, IPermissionService permissions)
    {
        _uow = uow;
        _permissions = permissions;
    }

    public async Task PostSaleAsync(DateTime date, string description, decimal amount, int saleId)
    {
        await _uow.JournalEntries.AddAsync(new JournalEntry
        {
            Date = date,
            Description = description,
            ReferenceType = "Sale",
            ReferenceId = saleId,
            Lines =
            [
                new() { AccountName = "الصندوق", DebitAmount = amount },
                new() { AccountName = "إيرادات المبيعات", CreditAmount = amount }
            ]
        });
    }

    public async Task PostPurchaseAsync(DateTime date, string description, decimal amount, int purchaseId)
    {
        await _uow.JournalEntries.AddAsync(new JournalEntry
        {
            Date = date,
            Description = description,
            ReferenceType = "Purchase",
            ReferenceId = purchaseId,
            Lines =
            [
                new() { AccountName = "المخزون", DebitAmount = amount },
                new() { AccountName = "الصندوق", CreditAmount = amount }
            ]
        });
    }

    public async Task PostExpenseAsync(DateTime date, string category, string description, decimal amount, int expenseId)
    {
        await _uow.JournalEntries.AddAsync(new JournalEntry
        {
            Date = date,
            Description = $"مصروف: {category} - {description}",
            ReferenceType = "Expense",
            ReferenceId = expenseId,
            Lines =
            [
                new() { AccountName = $"مصروف {category}", DebitAmount = amount },
                new() { AccountName = "الصندوق", CreditAmount = amount }
            ]
        });
    }

    public async Task PostRepairDeliveryAsync(DateTime date, string ticketNumber, decimal totalCost, decimal partsCost, int ticketId)
    {
        // totalCost is the full amount charged to the customer (parts + labor already combined) -
        // it is the revenue for this line; partsCost is posted separately below as COGS, not added here.
        await _uow.JournalEntries.AddAsync(new JournalEntry
        {
            Date = date,
            Description = $"صيانة: {ticketNumber} - {totalCost:N2}",
            ReferenceType = "RepairDelivery",
            ReferenceId = ticketId,
            Lines =
            [
                new() { AccountName = "الصندوق", DebitAmount = totalCost },
                new() { AccountName = "إيرادات الصيانة", CreditAmount = totalCost }
            ]
        });

        if (partsCost > 0)
        {
            await _uow.JournalEntries.AddAsync(new JournalEntry
            {
                Date = date,
                Description = $"تكلفة قطع صيانة: {ticketNumber}",
                ReferenceType = "RepairParts",
                ReferenceId = ticketId,
                Lines =
                [
                    new() { AccountName = "تكلفة الصيانة", DebitAmount = partsCost },
                    new() { AccountName = "المخزون", CreditAmount = partsCost }
                ]
            });
        }
    }

    public async Task PostSupplierPaymentAsync(DateTime date, string supplierName, decimal amount, int paymentId)
    {
        await _uow.JournalEntries.AddAsync(new JournalEntry
        {
            Date = date,
            Description = $"دفعة مورد: {supplierName} - {amount:N2}",
            ReferenceType = "SupplierPayment",
            ReferenceId = paymentId,
            Lines =
            [
                new() { AccountName = "حسابات الدفع", DebitAmount = amount },
                new() { AccountName = "الصندوق", CreditAmount = amount }
            ]
        });
    }

    public async Task PostDamagedAsync(DateTime date, string itemName, int quantity, decimal value, int movementId)
    {
        await _uow.JournalEntries.AddAsync(new JournalEntry
        {
            Date = date,
            Description = $"تالف: {itemName} x{quantity}",
            ReferenceType = "Damaged",
            ReferenceId = movementId,
            Lines =
            [
                new() { AccountName = "خسائر التوالف", DebitAmount = value },
                new() { AccountName = "المخزون", CreditAmount = value }
            ]
        });
    }

    // ========== Query methods ==========

    public async Task<IEnumerable<JournalEntryDto>> GetAllEntriesAsync()
    {
        _permissions.Require(Permission.ViewReports);
        var entries = await _uow.JournalEntries.Query()
            .Include(je => je.Lines)
            .OrderByDescending(je => je.Date).ThenByDescending(je => je.Id)
            .ToListAsync();

        return entries.Select(MapToDto);
    }

    public async Task<JournalEntryDto?> GetEntryByIdAsync(int id)
    {
        _permissions.Require(Permission.ViewReports);
        var entry = await _uow.JournalEntries.Query()
            .Include(je => je.Lines)
            .FirstOrDefaultAsync(je => je.Id == id);

        return entry is null ? null : MapToDto(entry);
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(DateTime? asOf = null)
    {
        _permissions.Require(Permission.ViewReports);
        var query = _uow.JournalEntryLines.Query();

        if (asOf.HasValue)
        {
            var end = asOf.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(jl => jl.JournalEntry.Date <= end);
        }

        var lines = await query.ToListAsync();

        var grouped = lines
            .GroupBy(jl => jl.AccountName)
            .Select(g => new TrialBalanceRowDto(
                g.Key,
                g.Sum(jl => jl.DebitAmount),
                g.Sum(jl => jl.CreditAmount),
                g.Sum(jl => jl.DebitAmount) - g.Sum(jl => jl.CreditAmount)))
            .OrderBy(r => r.AccountName)
            .ToList();

        var grandDebits = grouped.Sum(r => r.TotalDebits);
        var grandCredits = grouped.Sum(r => r.TotalCredits);

        return new TrialBalanceDto(grouped, grandDebits, grandCredits, grandDebits == grandCredits);
    }

    private static JournalEntryDto MapToDto(JournalEntry je) => new(
        je.Id, je.Date, je.Description, je.ReferenceType, je.ReferenceId, je.CreatedAt,
        je.Lines.Select(l => new JournalEntryLineDto(l.Id, l.AccountName, l.DebitAmount, l.CreditAmount)).ToList());
}
