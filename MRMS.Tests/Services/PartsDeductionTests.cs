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

/// <summary>
/// Phase 4.3 - deduction happens once, at the moment a part is attached to a ticket, regardless of the
/// ticket's status; removal restores it. Neither is triggered by a status transition any more.
/// </summary>
public sealed class PartsDeductionTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _itemId;
    private int _ticketId;

    public PartsDeductionTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        var item = new Item { Code = "P1", Name = "شاشة", CostPrice = 100, SellingPrice = 150, CurrentStock = 10 };
        ctx.Items.Add(item);
        var customer = new Customer { FullName = "ع", PhoneNumber = "0" };
        var device = new Device { Brand = "S", Model = "A", Customer = customer };
        var ticket = new RepairTicket
        {
            TicketNumber = "RPR-1", Device = device, Status = TicketStatus.Received,
            ReceiveDate = DateTime.UtcNow.Date, ReceiveTime = TimeSpan.Zero
        };
        ctx.RepairTickets.Add(ticket);
        ctx.SaveChanges();
        _itemId = item.Id;
        _ticketId = ticket.Id;
    }

    public void Dispose() => _connection.Dispose();

    private RepairTicketService NewService(UserRole role = UserRole.Owner)
    {
        var uow = new UnitOfWork(new AppDbContext(_options));
        var current = FakeCurrentUserService.As(role);
        var perms = new PermissionService(current);
        return new RepairTicketService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms), perms, new FakeCashSessionService(), current);
    }

    [Fact]
    public async Task Adding_a_part_deducts_stock_immediately_even_though_the_ticket_is_not_InRepair()
    {
        var part = await NewService().AddPartAsync(new AddPartToTicketDto(_ticketId, _itemId, 3, 100));

        await using var ctx = new AppDbContext(_options);
        (await ctx.Items.FindAsync(_itemId))!.CurrentStock.Should().Be(7);
        (await ctx.RepairTicketItems.FindAsync(part.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task Removing_a_part_restores_stock()
    {
        var service = NewService();
        var part = await service.AddPartAsync(new AddPartToTicketDto(_ticketId, _itemId, 4, 100));

        await service.RemovePartAsync(part.Id);

        await using var ctx = new AppDbContext(_options);
        (await ctx.Items.FindAsync(_itemId))!.CurrentStock.Should().Be(10);
        (await ctx.RepairTicketItems.FindAsync(part.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Cycling_the_ticket_through_InRepair_does_not_touch_stock_again()
    {
        var service = NewService();
        await service.AddPartAsync(new AddPartToTicketDto(_ticketId, _itemId, 2, 100));

        // Received -> UnderInspection -> InRepair -> WaitingForParts -> InRepair
        (await service.UpdateStatusAsync(new(_ticketId, TicketStatus.UnderInspection))).Should().BeTrue();
        (await service.UpdateStatusAsync(new(_ticketId, TicketStatus.InRepair))).Should().BeTrue();
        (await service.UpdateStatusAsync(new(_ticketId, TicketStatus.WaitingForParts))).Should().BeTrue();
        (await service.UpdateStatusAsync(new(_ticketId, TicketStatus.InRepair))).Should().BeTrue();

        await using var ctx = new AppDbContext(_options);
        (await ctx.Items.FindAsync(_itemId))!.CurrentStock.Should().Be(8, "the part was deducted exactly once, at AddPartAsync");
    }

    [Fact]
    public async Task Adding_more_than_available_stock_is_rejected()
    {
        await FluentActions.Awaiting(() => NewService().AddPartAsync(new AddPartToTicketDto(_ticketId, _itemId, 999, 100)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AddPart_and_RemovePart_require_DeductParts_permission()
    {
        await FluentActions.Awaiting(() => NewService(UserRole.Receptionist).AddPartAsync(new AddPartToTicketDto(_ticketId, _itemId, 1, 100)))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
