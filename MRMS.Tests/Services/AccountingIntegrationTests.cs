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

/// <summary>
/// End-to-end coverage for Phase 0.1 / 0.2: a sale must persist its journal entry (shared DbContext)
/// and a mid-operation failure must roll the whole thing back (real transaction). Uses a real SQLite
/// database so BeginTransaction actually works - EF InMemory would silently no-op transactions.
/// </summary>
public sealed class AccountingIntegrationTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AccountingIntegrationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext NewContext() => new(_options);

    private static readonly ICurrentUserService Owner = new FakeCurrentUserService();
    private static IPermissionService Perms => TestServices.Permissions(Owner);
    private static IAuditService Audit(IUnitOfWork uow) => new AuditService(uow, Owner, Perms);
    private static JournalService Journal(IUnitOfWork uow) => new(uow, Perms);
    private static SystemSettingService Settings(IUnitOfWork uow) => new(uow, Perms);
    private static SaleService Sale(IUnitOfWork uow) =>
        new(uow, Journal(uow), Audit(uow), Perms, Owner, Settings(uow), TestServices.CashSessions());
    private static SaleService Sale(IUnitOfWork uow, IJournalService journal) =>
        new(uow, journal, Audit(uow), Perms, Owner, Settings(uow), TestServices.CashSessions());
    private static RepairTicketService Repair(IUnitOfWork uow) =>
        new(uow, Journal(uow), Audit(uow), Perms, TestServices.CashSessions(), Owner);

    [Fact]
    public async Task SeedService_brings_up_a_usable_schema_and_baseline_on_a_fresh_database()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            await new MRMS.Infrastructure.Services.SeedService(new AppDbContext(options)).SeedAsync();

            await using var ctx = new AppDbContext(options);
            (await ctx.Users.CountAsync()).Should().Be(1);
            (await ctx.SystemSettings.AnyAsync(s => s.Key == "PaymentMethods")).Should().BeTrue();
            (await ctx.Items.AnyAsync()).Should().BeFalse("no demo data is seeded");
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Sale_persists_a_balanced_journal_entry_and_decrements_stock()
    {
        int itemId;
        await using (var seed = NewContext())
        {
            var item = new Item { Code = "ACC-1", Name = "جراب", CostPrice = 30, SellingPrice = 50, CurrentStock = 10 };
            seed.Items.Add(item);
            await seed.SaveChangesAsync();
            itemId = item.Id;
        }

        await using (var ctx = NewContext())
        {
            var uow = new UnitOfWork(ctx);
            var sales = Sale(uow);

            await sales.CreateAsync(new CreateSaleDto(
                DateTime.Today, itemId, Quantity: 3, UnitPrice: 50, CostPrice: 30,
                CustomerName: "عميل", Notes: ""));
        }

        await using (var assert = NewContext())
        {
            var entries = await assert.JournalEntries.Include(e => e.Lines).ToListAsync();
            entries.Should().HaveCount(1);
            var lines = entries[0].Lines;
            lines.Should().HaveCount(2);
            lines.Sum(l => l.DebitAmount).Should().Be(150m);
            lines.Sum(l => l.CreditAmount).Should().Be(150m);
            lines.Sum(l => l.DebitAmount).Should().Be(lines.Sum(l => l.CreditAmount));

            (await assert.Items.FindAsync(itemId))!.CurrentStock.Should().Be(7);

            var audit = await assert.AuditLogs.SingleAsync();
            audit.Action.Should().Be("Sale.Create");
            audit.UserName.Should().Be("المالك");
        }
    }

    [Fact]
    public async Task Sale_that_fails_after_first_save_rolls_back_everything()
    {
        int itemId;
        await using (var seed = NewContext())
        {
            var item = new Item { Code = "ACC-2", Name = "شاحن", CostPrice = 20, SellingPrice = 40, CurrentStock = 5 };
            seed.Items.Add(item);
            await seed.SaveChangesAsync();
            itemId = item.Id;
        }

        await using (var ctx = NewContext())
        {
            var uow = new UnitOfWork(ctx);
            // Journal service pointed at a poisoned context so the second SaveChanges throws AFTER the
            // Sale row + stock change have already been written once inside the transaction.
            var sales = Sale(uow, new ThrowingJournalService());

            var act = async () => await sales.CreateAsync(new CreateSaleDto(
                DateTime.Today, itemId, Quantity: 2, UnitPrice: 40, CostPrice: 20, CustomerName: "x", Notes: ""));

            await act.Should().ThrowAsync<Exception>();
        }

        await using (var assert = NewContext())
        {
            (await assert.Sales.AnyAsync()).Should().BeFalse("the transaction rolled back");
            (await assert.JournalEntries.AnyAsync()).Should().BeFalse();
            (await assert.Items.FindAsync(itemId))!.CurrentStock.Should().Be(5, "stock decrement rolled back too");
        }
    }

    [Fact]
    public async Task Delivering_a_ticket_sets_timestamps_and_posts_a_balanced_journal_entry()
    {
        int ticketId;
        await using (var seed = NewContext())
        {
            var customer = new Customer { FullName = "عميل", PhoneNumber = "0100" };
            var device = new Device { Brand = "Samsung", Model = "A10", Customer = customer };
            var ticket = new RepairTicket
            {
                TicketNumber = "RPR-00001",
                Device = device,
                Status = TicketStatus.Repaired,
                ReceiveDate = DateTime.UtcNow.Date,
                ReceiveTime = TimeSpan.Zero,
                Cost = 200m,
            };
            seed.RepairTickets.Add(ticket);
            await seed.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        await using (var ctx = NewContext())
        {
            var uow = new UnitOfWork(ctx);
            var service = Repair(uow);

            // Cash-basis revenue is recognized as payments are actually collected, not at delivery -
            // so the full amount must be collected first for the remaining balance to reach zero.
            await service.CollectPaymentAsync(new CollectPaymentDto(ticketId, 200m));

            var ok = await service.UpdateStatusAsync(
                new UpdateRepairTicketStatusDto(ticketId, TicketStatus.Delivered));
            ok.Should().BeTrue();
        }

        await using (var assert = NewContext())
        {
            var ticket = await assert.RepairTickets.FindAsync(ticketId);
            ticket!.DeliveryDate.Should().Be(DateTime.UtcNow.Date);
            ticket.DeliveryTime.Should().NotBeNull();
            ticket.PaidAmount.Should().Be(200m);

            var lines = await assert.JournalEntryLines.ToListAsync();
            lines.Should().NotBeEmpty();
            lines.Sum(l => l.DebitAmount).Should().Be(lines.Sum(l => l.CreditAmount));
        }
    }

    [Fact]
    public async Task Collecting_a_payment_posts_only_the_amount_paid_not_the_full_cost()
    {
        int ticketId;
        await using (var seed = NewContext())
        {
            var customer = new Customer { FullName = "عميل", PhoneNumber = "0100" };
            var device = new Device { Brand = "Samsung", Model = "A10", Customer = customer };
            var ticket = new RepairTicket
            {
                TicketNumber = "RPR-00002",
                Device = device,
                Status = TicketStatus.Repaired,
                ReceiveDate = DateTime.UtcNow.Date,
                ReceiveTime = TimeSpan.Zero,
                Cost = 500m,
            };
            seed.RepairTickets.Add(ticket);
            await seed.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        await using (var ctx = NewContext())
        {
            var uow = new UnitOfWork(ctx);
            var service = Repair(uow);
            var dto = await service.CollectPaymentAsync(new CollectPaymentDto(ticketId, 150m));
            dto.PaidAmount.Should().Be(150m);
            dto.RemainingAmount.Should().Be(350m);
        }

        await using (var assert = NewContext())
        {
            var lines = await assert.JournalEntryLines.ToListAsync();
            lines.Sum(l => l.DebitAmount).Should().Be(150m, "only the amount actually collected is posted, not the full 500 cost");
            lines.Sum(l => l.CreditAmount).Should().Be(150m);
        }
    }

    private sealed class ThrowingJournalService : MRMS.Application.Interfaces.IJournalService
    {
        public Task PostSaleAsync(DateTime date, string description, decimal amount, int saleId)
            => throw new InvalidOperationException("boom");
        public Task PostPurchaseAsync(DateTime date, string description, decimal amount, int purchaseId) => Task.CompletedTask;
        public Task PostExpenseAsync(DateTime date, string category, string description, decimal amount, int expenseId) => Task.CompletedTask;
        public Task PostRepairPaymentAsync(DateTime date, string ticketNumber, decimal amount, int ticketId) => Task.CompletedTask;
        public Task PostRepairPartsCostAsync(DateTime date, string ticketNumber, decimal partsCost, int ticketId) => Task.CompletedTask;
        public Task PostSupplierPaymentAsync(DateTime date, string supplierName, decimal amount, int paymentId) => Task.CompletedTask;
        public Task PostDamagedAsync(DateTime date, string itemName, int quantity, decimal value, int movementId) => Task.CompletedTask;
        public Task PostReversalAsync(string referenceType, int referenceId, DateTime date, string reason) => Task.CompletedTask;
        public Task<IEnumerable<JournalEntryDto>> GetAllEntriesAsync() => Task.FromResult(Enumerable.Empty<JournalEntryDto>());
        public Task<JournalEntryDto?> GetEntryByIdAsync(int id) => Task.FromResult<JournalEntryDto?>(null);
        public Task<TrialBalanceDto> GetTrialBalanceAsync(DateTime? asOf = null)
            => Task.FromResult(new TrialBalanceDto(new List<TrialBalanceRowDto>(), 0, 0, true));
    }
}
