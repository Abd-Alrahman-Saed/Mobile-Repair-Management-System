using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Infrastructure.Data;
using MRMS.Infrastructure.Repositories;

namespace MRMS.Tests.Services;

/// <summary>Covers 4.9: PaidAmount/RemainingAmount tracking, cash-basis journal posting per payment
/// (not the full ticket cost), and the owner-approval gate on delivering a ticket with a remaining
/// balance.</summary>
public sealed class PaymentCollectionTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _ticketId;
    private int _ownerId;
    private int _managerId;

    public PaymentCollectionTests()
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
            ReceiveDate = DateTime.UtcNow.Date, ReceiveTime = TimeSpan.Zero, Cost = 300m
        };
        ctx.RepairTickets.Add(ticket);

        var owner = new User { Username = "own", FullName = "المالك", Role = UserRole.Owner, IsActive = true, PasswordHash = "x", PasswordSalt = "y" };
        var manager = new User { Username = "man", FullName = "مدير", Role = UserRole.Manager, IsActive = true, PasswordHash = "x", PasswordSalt = "y" };
        ctx.Users.AddRange(owner, manager);
        ctx.SaveChanges();

        _ticketId = ticket.Id;
        _ownerId = owner.Id;
        _managerId = manager.Id;
    }

    public void Dispose() => _connection.Dispose();

    private RepairTicketService NewTicketService(UserRole role = UserRole.Owner)
    {
        var uow = new UnitOfWork(new AppDbContext(_options));
        var perms = TestServices.Permissions(role);
        var current = FakeCurrentUserService.As(role);
        return new RepairTicketService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms), perms);
    }

    [Fact]
    public async Task Collecting_a_payment_increases_PaidAmount_and_posts_only_that_amount()
    {
        var dto = await NewTicketService().CollectPaymentAsync(new CollectPaymentDto(_ticketId, 100m));

        dto.PaidAmount.Should().Be(100m);
        dto.RemainingAmount.Should().Be(200m);

        await using var ctx = new AppDbContext(_options);
        (await ctx.RepairTickets.FindAsync(_ticketId))!.PaidAmount.Should().Be(100m);
        (await ctx.AuditLogs.AnyAsync(a => a.Action == "RepairTicket.PaymentCollected")).Should().BeTrue();

        var lines = await ctx.JournalEntryLines.ToListAsync();
        lines.Sum(l => l.DebitAmount).Should().Be(100m);
        lines.Sum(l => l.CreditAmount).Should().Be(100m);
    }

    [Fact]
    public async Task Collecting_a_non_positive_amount_is_rejected()
    {
        await FluentActions.Awaiting(() => NewTicketService().CollectPaymentAsync(new CollectPaymentDto(_ticketId, 0m)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Delivering_with_a_remaining_balance_and_no_approver_is_rejected()
    {
        var service = NewTicketService();

        var act = () => service.UpdateStatusAsync(new UpdateRepairTicketStatusDto(_ticketId, TicketStatus.Delivered));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*موافقة المالك*");
    }

    [Fact]
    public async Task Delivering_with_a_remaining_balance_and_a_valid_owner_approver_succeeds()
    {
        var service = NewTicketService();

        var ok = await service.UpdateStatusAsync(
            new UpdateRepairTicketStatusDto(_ticketId, TicketStatus.Delivered, ApprovedByUserId: _ownerId));

        ok.Should().BeTrue();
        await using var ctx = new AppDbContext(_options);
        (await ctx.RepairTickets.FindAsync(_ticketId))!.Status.Should().Be(TicketStatus.Delivered);
    }

    [Fact]
    public async Task Delivering_with_an_approver_lacking_the_permission_is_still_rejected()
    {
        var service = NewTicketService();

        var act = () => service.UpdateStatusAsync(
            new UpdateRepairTicketStatusDto(_ticketId, TicketStatus.Delivered, ApprovedByUserId: _managerId));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*موافقة المالك*");
    }

    [Fact]
    public async Task Delivering_once_the_full_amount_is_collected_needs_no_approval()
    {
        var service = NewTicketService();
        await service.CollectPaymentAsync(new CollectPaymentDto(_ticketId, 300m));

        var ok = await service.UpdateStatusAsync(new UpdateRepairTicketStatusDto(_ticketId, TicketStatus.Delivered));

        ok.Should().BeTrue();
    }
}
