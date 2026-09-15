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

/// <summary>Phase 7.1 - a Service item (e.g. "خدمة تركيب شاشة") carries no stock: CurrentStock stays
/// null, it's never blocked for "insufficient stock", and it never shows up in low/out-of-stock
/// alerts or stock-value calculations.</summary>
public sealed class ServiceItemTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public ServiceItemTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private IUnitOfWork Uow() => new UnitOfWork(new AppDbContext(_options));

    private ItemService Items(UserRole role = UserRole.Owner) =>
        new(Uow(), TestServices.Permissions(role));

    private SaleService Sales()
    {
        var uow = Uow();
        var current = FakeCurrentUserService.As(UserRole.Owner);
        var perms = TestServices.Permissions(current);
        return new SaleService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, current, new SystemSettingService(uow, perms), new FakeCashSessionService());
    }

    private RepairTicketService Repair()
    {
        var uow = Uow();
        var current = FakeCurrentUserService.As(UserRole.Owner);
        var perms = TestServices.Permissions(current);
        return new RepairTicketService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, new FakeCashSessionService(), current);
    }

    [Fact]
    public async Task Creating_a_service_item_starts_with_a_null_stock()
    {
        var item = await Items().CreateAsync(new CreateItemDto(
            "SVC-1", "خدمة تركيب شاشة", ItemType.Service, "خدمة", 0, 150, 0));

        item.CurrentStock.Should().BeNull();
    }

    [Fact]
    public async Task Selling_a_service_item_in_any_quantity_never_fails_and_leaves_stock_null()
    {
        var item = await Items().CreateAsync(new CreateItemDto(
            "SVC-1", "خدمة تركيب شاشة", ItemType.Service, "خدمة", 0, 150, 0));

        var invoice = await Sales().CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, null, "x", PaymentMethod.Cash,
            [new CreateSalesInvoiceLineDto(item.Id, 1000, 150m)], 0, 150_000m, ""));

        invoice.Total.Should().Be(150_000m);

        await using var ctx = new AppDbContext(_options);
        (await ctx.Items.FindAsync(item.Id))!.CurrentStock.Should().BeNull();
    }

    [Fact]
    public async Task Attaching_a_service_item_as_a_repair_part_never_fails_and_leaves_stock_null()
    {
        int ticketId, serviceItemId;
        await using (var ctx = new AppDbContext(_options))
        {
            var svc = new Item { Code = "SVC-2", Name = "خدمة تركيب", Type = ItemType.Service, CurrentStock = null };
            var customer = new Customer { FullName = "ع", PhoneNumber = "0" };
            var device = new Device { Brand = "S", Model = "A", Customer = customer };
            var ticket = new RepairTicket
            {
                TicketNumber = "RPR-1", Device = device, Status = TicketStatus.Received,
                ReceiveDate = DateTime.UtcNow.Date, ReceiveTime = TimeSpan.Zero
            };
            ctx.Items.Add(svc);
            ctx.RepairTickets.Add(ticket);
            await ctx.SaveChangesAsync();
            ticketId = ticket.Id;
            serviceItemId = svc.Id;
        }

        await Repair().AddPartAsync(new AddPartToTicketDto(ticketId, serviceItemId, 5, 0));

        await using var assert = new AppDbContext(_options);
        (await assert.Items.FindAsync(serviceItemId))!.CurrentStock.Should().BeNull();
    }

    [Fact]
    public async Task GetLowStockAsync_never_includes_a_service_item()
    {
        await using (var ctx = new AppDbContext(_options))
        {
            ctx.Items.Add(new Item { Code = "SVC-3", Name = "خدمة", Type = ItemType.Service, CurrentStock = null, ReorderLevel = 999 });
            await ctx.SaveChangesAsync();
        }

        var lowStock = await Items().GetLowStockAsync();

        lowStock.Should().BeEmpty();
    }
}
