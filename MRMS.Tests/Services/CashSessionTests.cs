using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;
using MRMS.Infrastructure.Data;
using MRMS.Infrastructure.Repositories;

namespace MRMS.Tests.Services;

/// <summary>Phase 5 — cash drawer / shift: open/close, expected-amount computation, same-day lock,
/// per-session attribution of repair payments, and history visibility.</summary>
public sealed class CashSessionTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _itemId;
    private int _ticketId;

    public CashSessionTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        var item = new Item { Code = "IT-1", Name = "شاشة", Unit = "قطعة", CostPrice = 100m, SellingPrice = 150m, CurrentStock = 50 };
        ctx.Items.Add(item);

        var customer = new Customer { FullName = "ع", PhoneNumber = "0100" };
        var device = new Device { Brand = "S", Model = "A", Customer = customer };
        var ticket = new RepairTicket
        {
            TicketNumber = "RPR-1", Device = device, Status = TicketStatus.Repaired,
            ReceiveDate = DateTime.UtcNow.Date, ReceiveTime = TimeSpan.Zero, Cost = 400m
        };
        ctx.RepairTickets.Add(ticket);
        ctx.SaveChanges();

        _itemId = item.Id;
        _ticketId = ticket.Id;
    }

    public void Dispose() => _connection.Dispose();

    private IUnitOfWork Uow() => new UnitOfWork(new AppDbContext(_options));

    private CashSessionService Cash(IUnitOfWork uow, UserRole role = UserRole.Owner)
    {
        var current = FakeCurrentUserService.As(role);
        var perms = TestServices.Permissions(current);
        return new CashSessionService(uow, new AuditService(uow, current, perms), perms, current);
    }

    private SaleService Sales(IUnitOfWork uow, ICashSessionService cash, UserRole role = UserRole.Owner)
    {
        var current = FakeCurrentUserService.As(role);
        var perms = TestServices.Permissions(current);
        return new SaleService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, current, new SystemSettingService(uow, perms), cash);
    }

    private ExpenseService Expenses(IUnitOfWork uow, ICashSessionService cash, UserRole role = UserRole.Owner)
    {
        var current = FakeCurrentUserService.As(role);
        var perms = TestServices.Permissions(current);
        return new ExpenseService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, current, cash);
    }

    private RepairTicketService Repair(IUnitOfWork uow, ICashSessionService cash, UserRole role = UserRole.Owner)
    {
        var current = FakeCurrentUserService.As(role);
        var perms = TestServices.Permissions(current);
        return new RepairTicketService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, cash, current);
    }

    [Fact]
    public async Task Opening_a_second_session_while_one_is_open_is_rejected()
    {
        await Cash(Uow()).OpenAsync(500m);

        await FluentActions.Awaiting(() => Cash(Uow()).OpenAsync(100m))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Expected_amount_is_opening_plus_cash_sales_plus_repair_collections_minus_cash_expenses()
    {
        await Cash(Uow()).OpenAsync(1000m);

        var uow = Uow();
        var cash = Cash(uow);
        await Sales(uow, cash).CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, null, "x", PaymentMethod.Cash,
            [new CreateSalesInvoiceLineDto(_itemId, 2, 150m)], 0, 300m, ""));   // +300
        await Expenses(uow, cash).CreateAsync(new CreateExpenseDto(DateTime.Today, "إيجار", "", 120m, PaymentMethod.Cash, ""));   // -120
        await Expenses(uow, cash).CreateAsync(new CreateExpenseDto(DateTime.Today, "آجل", "", 999m, PaymentMethod.Credit, ""));   // ignored
        await Repair(uow, cash).CollectPaymentAsync(new CollectPaymentDto(_ticketId, 250m));   // +250

        var current = await Cash(Uow()).GetCurrentAsync();
        current.Should().NotBeNull();
        current!.CashSales.Should().Be(300m);
        current.CashExpenses.Should().Be(120m);
        current.RepairCollections.Should().Be(250m);
        current.ExpectedAmount.Should().Be(1000m + 300m - 120m + 250m);
    }

    [Fact]
    public async Task Closing_freezes_the_difference_with_the_correct_sign()
    {
        await Cash(Uow()).OpenAsync(1000m);

        var uow = Uow();
        await Sales(uow, Cash(uow)).CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, null, "x", PaymentMethod.Cash,
            [new CreateSalesInvoiceLineDto(_itemId, 1, 150m)], 0, 150m, ""));

        // expected = 1150; count 1100 → shortage of 50
        var closed = await Cash(Uow()).CloseAsync(1100m, "عجز");
        closed.IsClosed.Should().BeTrue();
        closed.ExpectedAmount.Should().Be(1150m);
        closed.CountedAmount.Should().Be(1100m);
        closed.Difference.Should().Be(-50m);
    }

    [Fact]
    public async Task After_close_a_receptionist_cannot_transact_the_same_day_but_an_owner_can()
    {
        await Cash(Uow(), UserRole.Receptionist).OpenAsync(0m);
        await Cash(Uow(), UserRole.Receptionist).CloseAsync(0m, "");

        var recUow = Uow();
        await FluentActions.Awaiting(() => Sales(recUow, Cash(recUow, UserRole.Receptionist), UserRole.Receptionist)
                .CreateAsync(new CreateSalesInvoiceDto(
                    DateTime.Today, null, "x", PaymentMethod.Cash,
                    [new CreateSalesInvoiceLineDto(_itemId, 1, 150m)], 0, 150m, "")))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*مقفلة*");

        var ownUow = Uow();
        var sale = await Sales(ownUow, Cash(ownUow)).CreateAsync(
            new CreateSalesInvoiceDto(
                DateTime.Today, null, "x", PaymentMethod.Cash,
                [new CreateSalesInvoiceLineDto(_itemId, 1, 150m)], 0, 150m, ""));
        sale.Should().NotBeNull();
    }

    [Fact]
    public async Task A_ticket_paid_across_two_shifts_splits_its_collections_between_them()
    {
        // Shift 1: deposit 100
        await Cash(Uow()).OpenAsync(0m);
        var u1 = Uow();
        await Repair(u1, Cash(u1)).CollectPaymentAsync(new CollectPaymentDto(_ticketId, 100m));
        var s1 = await Cash(Uow()).CloseAsync(100m, "");

        // Shift 2: final 300
        await Cash(Uow()).OpenAsync(0m);
        var u2 = Uow();
        await Repair(u2, Cash(u2)).CollectPaymentAsync(new CollectPaymentDto(_ticketId, 300m));
        var s2 = await Cash(Uow()).GetCurrentAsync();

        s1.RepairCollections.Should().Be(100m);
        s2!.RepairCollections.Should().Be(300m);
    }

    [Fact]
    public async Task History_is_scoped_to_own_shifts_for_a_receptionist_and_global_for_an_owner()
    {
        await Cash(Uow(), UserRole.Receptionist).OpenAsync(0m);
        await Cash(Uow(), UserRole.Receptionist).CloseAsync(0m, "");

        (await Cash(Uow(), UserRole.Receptionist).GetHistoryAsync()).Should().ContainSingle();
        (await Cash(Uow(), UserRole.Owner).GetHistoryAsync()).Should().ContainSingle();

        // A different receptionist (id differs) sees none.
        var otherCurrent = new FakeCurrentUserService();
        otherCurrent.SetCurrentUser(new CurrentUser(99, "other", "Other", UserRole.Receptionist));
        var uow = Uow();
        var perms = TestServices.Permissions(otherCurrent);
        var svc = new CashSessionService(uow, new AuditService(uow, otherCurrent, perms), perms, otherCurrent);
        (await svc.GetHistoryAsync()).Should().BeEmpty();
    }
}
