using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Infrastructure.Data;
using MRMS.Infrastructure.Repositories;

namespace MRMS.Tests.Services;

/// <summary>Phase 2.4 - cost/profit are stripped from the DTO for roles that can't see them.</summary>
public sealed class FieldHidingTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public FieldHidingTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        ctx.Items.Add(new Item { Code = "A1", Name = "جراب", CostPrice = 30, SellingPrice = 50, CurrentStock = 10 });
        ctx.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private ItemService Items(UserRole role) =>
        new(new UnitOfWork(new AppDbContext(_options)), new PermissionService(FakeCurrentUserService.As(role)));

    private SaleService Sales(UserRole role)
    {
        var uow = new UnitOfWork(new AppDbContext(_options));
        var current = FakeCurrentUserService.As(role);
        var perms = new PermissionService(current);
        return new SaleService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, current, new SystemSettingService(uow, perms));
    }

    [Fact]
    public async Task Receptionist_gets_null_cost_from_items()
    {
        var item = (await Items(UserRole.Receptionist).GetAllAsync()).Single();
        item.CostPrice.Should().BeNull();
    }

    [Fact]
    public async Task Owner_gets_the_real_cost_from_items()
    {
        var item = (await Items(UserRole.Owner).GetAllAsync()).Single();
        item.CostPrice.Should().Be(30m);
    }

    [Fact]
    public async Task Sale_locks_cost_from_the_item_and_hides_profit_from_receptionist()
    {
        int itemId;
        await using (var ctx = new AppDbContext(_options))
            itemId = (await ctx.Items.SingleAsync()).Id;

        // Receptionist creates a sale; their form can't see cost so it passes 0 - the service must ignore it.
        await Sales(UserRole.Receptionist).CreateAsync(new CreateSaleDto(
            DateTime.Today, itemId, Quantity: 2, UnitPrice: 50, CostPrice: 0, CustomerName: "x", Notes: ""));

        await using (var ctx = new AppDbContext(_options))
            (await ctx.Sales.SingleAsync()).CostPrice.Should().Be(30m, "cost is taken from the item, not the caller");

        var asReception = (await Sales(UserRole.Receptionist).GetAllAsync()).Single();
        asReception.Profit.Should().BeNull();
        asReception.CostPrice.Should().BeNull();

        var asOwner = (await Sales(UserRole.Owner).GetAllAsync()).Single();
        asOwner.Profit.Should().Be(40m); // (50-30) * 2
    }
}
