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

public sealed class AlertServiceTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AlertServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        ctx.SystemSettings.Add(new SystemSetting { Key = "Alerts.StaleTicketDays", Value = "3" });
        ctx.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private AlertService NewService()
    {
        var uow = new UnitOfWork(new AppDbContext(_options));
        var perms = TestServices.Permissions();
        return new AlertService(uow, new SystemSettingService(uow, perms));
    }

    private async Task SeedItemAsync(int stock, int reorder)
    {
        await using var ctx = new AppDbContext(_options);
        ctx.Items.Add(new Item { Code = $"I{Guid.NewGuid():N}"[..8], Name = "قطعة", CurrentStock = stock, ReorderLevel = reorder });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Out_of_stock_low_stock_and_healthy_items()
    {
        await SeedItemAsync(stock: 0, reorder: 2);
        await SeedItemAsync(stock: 2, reorder: 5);
        await SeedItemAsync(stock: 50, reorder: 5);

        var alerts = await NewService().GetAlertsAsync();

        alerts.Should().ContainSingle(a => a.Kind == "OutOfStock");
        alerts.Should().ContainSingle(a => a.Kind == "LowStock");
        alerts.Count(a => a.Kind == "OutOfStock" || a.Kind == "LowStock").Should().Be(2, "the healthy item raises nothing");
    }

    [Fact]
    public async Task A_service_item_never_raises_a_stock_alert()
    {
        await using (var ctx = new AppDbContext(_options))
        {
            ctx.Items.Add(new Item { Code = "SVC-1", Name = "خدمة تركيب شاشة", Type = ItemType.Service,
                CurrentStock = null, ReorderLevel = 999 });
            await ctx.SaveChangesAsync();
        }

        var alerts = await NewService().GetAlertsAsync();

        alerts.Should().NotContain(a => a.Kind == "OutOfStock" || a.Kind == "LowStock");
    }

    [Fact]
    public async Task Stale_waiting_for_parts_ticket_raises_an_alert()
    {
        await using (var ctx = new AppDbContext(_options))
        {
            var customer = new Customer { FullName = "ع", PhoneNumber = "0" };
            var device = new Device { Brand = "S", Model = "A", Customer = customer };
            ctx.RepairTickets.AddRange(
                new RepairTicket { TicketNumber = "OLD", Device = device, Status = TicketStatus.WaitingForParts,
                    ReceiveDate = DateTime.UtcNow.Date.AddDays(-10), ReceiveTime = TimeSpan.Zero },
                new RepairTicket { TicketNumber = "NEW", Device = device, Status = TicketStatus.WaitingForParts,
                    ReceiveDate = DateTime.UtcNow.Date, ReceiveTime = TimeSpan.Zero });
            await ctx.SaveChangesAsync();
        }

        var alerts = await NewService().GetAlertsAsync();

        alerts.Should().ContainSingle(a => a.Kind == "StaleTicket");
        alerts.Single(a => a.Kind == "StaleTicket").Title.Should().Contain("OLD");
    }
}
