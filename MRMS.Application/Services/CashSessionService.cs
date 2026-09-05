using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Application.Services;

public class CashSessionService : ICashSessionService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUserService _currentUser;

    public CashSessionService(IUnitOfWork uow, IAuditService audit, IPermissionService permissions,
        ICurrentUserService currentUser)
    {
        _uow = uow;
        _audit = audit;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<CashSessionDto?> GetCurrentAsync()
    {
        var session = await _uow.CashSessions.Query()
            .FirstOrDefaultAsync(s => s.ClosedAt == null);
        return session is null ? null : await MapToDtoAsync(session);
    }

    public async Task<int?> GetCurrentSessionIdAsync()
    {
        var session = await _uow.CashSessions.Query()
            .Where(s => s.ClosedAt == null)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
        return session;
    }

    public async Task<CashSessionDto> OpenAsync(decimal openingBalance)
    {
        _permissions.Require(Permission.ViewCashRegister);

        if (openingBalance < 0)
            throw new InvalidOperationException("الرصيد الافتتاحي لا يمكن أن يكون سالباً");

        if (await _uow.CashSessions.Query().AnyAsync(s => s.ClosedAt == null))
            throw new InvalidOperationException("توجد وردية مفتوحة بالفعل - يجب إقفالها أولاً");

        var user = _currentUser.User;
        var session = new CashSession
        {
            // Business date follows the same local-"today" convention as Sale.Date / Expense.Date.
            BusinessDate = DateTime.Today,
            OpenedByUserId = user?.Id ?? 0,
            OpenedByName = user?.FullName ?? user?.Username ?? "",
            OpenedAt = DateTime.UtcNow,
            OpeningBalance = openingBalance
        };

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            await _uow.CashSessions.AddAsync(session);
            await _audit.LogAsync("CashSession.Open", nameof(CashSession), session.Id,
                $"رصيد افتتاحي {openingBalance:N2}");
            await _uow.CompleteAsync();
        });

        return await MapToDtoAsync(session);
    }

    public async Task<CashSessionDto> CloseAsync(decimal countedAmount, string notes)
    {
        _permissions.Require(Permission.ViewCashRegister);

        var session = await _uow.CashSessions.Query().FirstOrDefaultAsync(s => s.ClosedAt == null)
            ?? throw new InvalidOperationException("لا توجد وردية مفتوحة للإقفال");

        var totals = await ComputeTotalsAsync(session);
        var user = _currentUser.User;

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            session.ExpectedAmount = totals.Expected;
            session.CountedAmount = countedAmount;
            session.Difference = countedAmount - totals.Expected;
            session.ClosedAt = DateTime.UtcNow;
            session.ClosedByUserId = user?.Id;
            session.ClosedByName = user?.FullName ?? user?.Username ?? "";
            session.Notes = notes?.Trim() ?? "";

            _uow.CashSessions.Update(session);
            await _audit.LogAsync("CashSession.Close", nameof(CashSession), session.Id,
                $"متوقّع {totals.Expected:N2} / معدود {countedAmount:N2} / فرق {session.Difference:N2}");
            await _uow.CompleteAsync();
        });

        return await MapToDtoAsync(session);
    }

    public async Task<IEnumerable<CashSessionDto>> GetHistoryAsync()
    {
        _permissions.Require(Permission.ViewCashRegister);

        var query = _uow.CashSessions.Query().Where(s => s.ClosedAt != null);

        // A receptionist sees only shifts they opened; an owner/manager (ViewReports) sees all.
        if (!_permissions.Can(Permission.ViewReports))
        {
            var uid = _currentUser.User?.Id ?? 0;
            query = query.Where(s => s.OpenedByUserId == uid);
        }

        var sessions = await query.OrderByDescending(s => s.OpenedAt).ToListAsync();

        var result = new List<CashSessionDto>();
        foreach (var s in sessions)
            result.Add(await MapToDtoAsync(s));
        return result;
    }

    public async Task EnsureNotLockedAsync(DateTime businessDate)
    {
        if (_currentUser.User?.Role == UserRole.Owner) return;

        var date = businessDate.Date;
        var locked = await _uow.CashSessions.Query()
            .AnyAsync(s => s.ClosedAt != null && s.BusinessDate == date);
        if (locked)
            throw new InvalidOperationException("اليومية مقفلة - لا يمكن تعديل حركات هذا اليوم");
    }

    private record SessionTotals(decimal CashSales, decimal RepairCollections, decimal CashExpenses)
    {
        public decimal Expected { get; init; }
    }

    private async Task<SessionTotals> ComputeTotalsAsync(CashSession s)
    {
        // Voided sales/expenses are already excluded by the global query filters.
        var cashSales = await _uow.Sales.Query()
            .Where(x => x.CashSessionId == s.Id)
            .SumAsync(x => (decimal?)(x.Quantity * x.UnitPrice)) ?? 0m;

        var repairCollections = await _uow.RepairTicketPayments.Query()
            .Where(p => p.CashSessionId == s.Id)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var cashExpenses = await _uow.Expenses.Query()
            .Where(x => x.CashSessionId == s.Id && x.PaymentMethod == PaymentMethod.Cash)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        return new SessionTotals(cashSales, repairCollections, cashExpenses)
        {
            Expected = s.OpeningBalance + cashSales + repairCollections - cashExpenses
        };
    }

    private async Task<CashSessionDto> MapToDtoAsync(CashSession s)
    {
        var t = await ComputeTotalsAsync(s);
        // For a closed session, the frozen figure is authoritative; for an open one, the live total.
        var expected = s.ExpectedAmount ?? t.Expected;
        return new CashSessionDto(
            s.Id, s.BusinessDate, s.OpenedByName, s.OpenedAt, s.OpeningBalance,
            t.CashSales, t.RepairCollections, t.CashExpenses, expected,
            s.ClosedByName, s.ClosedAt, s.CountedAmount, s.Difference, s.Notes, s.IsClosed);
    }
}
