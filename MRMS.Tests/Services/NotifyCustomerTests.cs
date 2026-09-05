using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Infrastructure.Data;
using MRMS.Infrastructure.Repositories;

namespace MRMS.Tests.Services;

/// <summary>Covers 4.8: marking a ticket as notified (WhatsApp "ready for pickup" message).</summary>
public sealed class NotifyCustomerTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _ticketId;

    public NotifyCustomerTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        var customer = new Customer { FullName = "ع", PhoneNumber = "0100" };
        var device = new Device { Brand = "S", Model = "A", Customer = customer };
        var ticket = new RepairTicket
        {
            TicketNumber = "RPR-1", Device = device, Status = TicketStatus.Repaired,
            ReceiveDate = DateTime.UtcNow.Date, ReceiveTime = TimeSpan.Zero, Cost = 100m
        };
        ctx.RepairTickets.Add(ticket);
        ctx.SaveChanges();

        _ticketId = ticket.Id;
    }

    public void Dispose() => _connection.Dispose();

    private RepairTicketService NewTicketService(UserRole role = UserRole.Receptionist)
    {
        var uow = new UnitOfWork(new AppDbContext(_options));
        var perms = TestServices.Permissions(role);
        var current = FakeCurrentUserService.As(role);
        return new RepairTicketService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms), perms, new FakeCashSessionService(), current);
    }

    [Fact]
    public async Task Marking_a_ticket_notified_sets_the_timestamp_and_is_audited()
    {
        await NewTicketService().MarkNotifiedAsync(_ticketId);

        await using var ctx = new AppDbContext(_options);
        (await ctx.RepairTickets.FindAsync(_ticketId))!.NotifiedAt.Should().NotBeNull();
        (await ctx.AuditLogs.AnyAsync(a => a.Action == "RepairTicket.CustomerNotified")).Should().BeTrue();
    }

    [Fact]
    public async Task Marking_a_nonexistent_ticket_throws()
    {
        await FluentActions.Awaiting(() => NewTicketService().MarkNotifiedAsync(999))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
