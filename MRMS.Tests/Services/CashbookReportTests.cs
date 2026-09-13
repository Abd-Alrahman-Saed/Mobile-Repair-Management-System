using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;
using MRMS.Infrastructure.Data;
using MRMS.Infrastructure.Repositories;

namespace MRMS.Tests.Services;

/// <summary>The cashbook must recognise repair cash from the payments actually collected on tickets
/// (RepairTicketPayment), not the ticket's full Cost at delivery - keeping it consistent with the
/// cash drawer (Phase 5) and incremental payment collection (Phase 4.9).</summary>
public sealed class CashbookReportTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _ticketId;

    public CashbookReportTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        var customer = new Customer { FullName = "ع", PhoneNumber = "0100" };
        var device = new Device { Brand = "Samsung", Model = "A10", Customer = customer };
        var ticket = new RepairTicket
        {
            TicketNumber = "RPR-1", Device = device, Status = TicketStatus.Repaired,
            ReceiveDate = DateTime.Today, ReceiveTime = TimeSpan.Zero, Cost = 500m
        };
        ctx.RepairTickets.Add(ticket);
        ctx.SaveChanges();
        _ticketId = ticket.Id;
    }

    public void Dispose() => _connection.Dispose();

    private IUnitOfWork Uow() => new UnitOfWork(new AppDbContext(_options));

    private RepairTicketService Repair(IUnitOfWork uow)
    {
        var current = FakeCurrentUserService.As(UserRole.Owner);
        var perms = TestServices.Permissions(current);
        return new RepairTicketService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, new FakeCashSessionService(), current);
    }

    private ReportService Report(IUnitOfWork uow) => new(uow, TestServices.Permissions(UserRole.Owner));

    [Fact]
    public async Task Cashbook_counts_repair_payments_not_the_full_ticket_cost()
    {
        // A 500-cost ticket, still in repair, only a 150 deposit collected.
        await Repair(Uow()).CollectPaymentAsync(new CollectPaymentDto(_ticketId, 150m));

        var report = await Report(Uow()).GetCashbookAsync(DateTime.Today.AddDays(-1), DateTime.Today);

        report.TotalInflows.Should().Be(150m, "only the deposit actually collected is cash in");
        report.Entries.Should().ContainSingle(e => e.Type == "وارد" && e.Amount == 150m
            && e.Description.Contains("صيانة"));
    }

    [Fact]
    public async Task Cashbook_opening_balance_uses_payments_collected_before_the_window()
    {
        // Two collections yesterday, then the window starts today.
        var yesterday = DateTime.Today.AddDays(-1);
        await using (var ctx = new AppDbContext(_options))
        {
            ctx.RepairTicketPayments.AddRange(
                new RepairTicketPayment { RepairTicketId = _ticketId, Amount = 100m, BusinessDate = yesterday, CollectedByName = "x" },
                new RepairTicketPayment { RepairTicketId = _ticketId, Amount = 50m, BusinessDate = yesterday, CollectedByName = "x" });
            await ctx.SaveChangesAsync();
        }

        var report = await Report(Uow()).GetCashbookAsync(DateTime.Today, DateTime.Today);

        report.OpeningBalance.Should().Be(150m);
        report.TotalInflows.Should().Be(0m);
    }
}
