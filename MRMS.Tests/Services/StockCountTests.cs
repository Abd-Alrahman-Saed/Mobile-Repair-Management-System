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

/// <summary>Phase 7.3 - stock-count adjustments (جرد): AdjustmentIncrease/AdjustmentDecrease move
/// the balance in the right direction, are documented under the acting user via the existing audit
/// log, are blocked for Service items, and require Permission.StockCount.</summary>
public sealed class StockCountTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _itemId;
    private int _serviceItemId;

    public StockCountTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        var item = new Item { Code = "A1", Name = "جراب", CostPrice = 30, SellingPrice = 50, CurrentStock = 10 };
        var service = new Item { Code = "SVC-1", Name = "خدمة", Type = ItemType.Service, CurrentStock = null };
        ctx.Items.AddRange(item, service);
        ctx.SaveChanges();
        _itemId = item.Id;
        _serviceItemId = service.Id;
    }

    public void Dispose() => _connection.Dispose();

    private IUnitOfWork Uow() => new UnitOfWork(new AppDbContext(_options));

    private StockMovementService Movements(UserRole role = UserRole.Manager)
    {
        var uow = Uow();
        var current = FakeCurrentUserService.As(role);
        var perms = TestServices.Permissions(current);
        return new StockMovementService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms), perms);
    }

    [Fact]
    public async Task A_count_found_more_than_the_book_balance_increases_stock()
    {
        // Book = 10, actual counted = 13 -> +3 AdjustmentIncrease
        await Movements().CreateAsync(new CreateStockMovementDto(
            DateTime.Today, MovementType.AdjustmentIncrease, _itemId, 3, 0m, null, "تسوية جرد"));

        await using var ctx = new AppDbContext(_options);
        (await ctx.Items.FindAsync(_itemId))!.CurrentStock.Should().Be(13);
        (await ctx.AuditLogs.SingleAsync()).UserName.Should().Be("Manager");
    }

    [Fact]
    public async Task A_count_found_less_than_the_book_balance_decreases_stock()
    {
        // Book = 10, actual counted = 6 -> -4 AdjustmentDecrease
        await Movements().CreateAsync(new CreateStockMovementDto(
            DateTime.Today, MovementType.AdjustmentDecrease, _itemId, 4, 0m, null, "تسوية جرد"));

        await using var ctx = new AppDbContext(_options);
        (await ctx.Items.FindAsync(_itemId))!.CurrentStock.Should().Be(6);
    }

    [Fact]
    public async Task A_movement_for_a_service_item_is_rejected()
    {
        var act = () => Movements().CreateAsync(new CreateStockMovementDto(
            DateTime.Today, MovementType.AdjustmentIncrease, _serviceItemId, 1, 0m, null, ""));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Creating_a_movement_without_StockCount_permission_is_rejected()
    {
        var act = () => Movements(UserRole.Receptionist).CreateAsync(new CreateStockMovementDto(
            DateTime.Today, MovementType.AdjustmentIncrease, _itemId, 1, 0m, null, ""));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
