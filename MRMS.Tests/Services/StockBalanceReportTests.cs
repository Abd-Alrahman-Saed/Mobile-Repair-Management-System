using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;
using MRMS.Infrastructure.Data;
using MRMS.Infrastructure.Repositories;

namespace MRMS.Tests.Services;

/// <summary>Phase 7.2 fix - GetStockBalanceAsync's comment always claimed cost/value were hidden
/// per-DTO for roles without ViewCostPrice, but the code never actually did it. Also covers the
/// Service item showing a null balance instead of 0.</summary>
public sealed class StockBalanceReportTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public StockBalanceReportTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        ctx.Items.AddRange(
            new Item { Code = "A1", Name = "جراب", CostPrice = 30, SellingPrice = 50, CurrentStock = 10, ReorderLevel = 2 },
            new Item { Code = "SVC-1", Name = "خدمة", Type = ItemType.Service, CurrentStock = null, ReorderLevel = 0 });
        ctx.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private ReportService Report(UserRole role) =>
        new(new UnitOfWork(new AppDbContext(_options)), TestServices.Permissions(role));

    [Fact]
    public async Task Cost_and_value_are_hidden_without_ViewCostPrice()
    {
        var rows = (await Report(UserRole.Receptionist).GetStockBalanceAsync()).ToList();

        rows.Should().OnlyContain(r => r.CostPrice == null && r.StockValue == null);
    }

    [Fact]
    public async Task Cost_and_value_are_shown_for_a_role_with_ViewCostPrice()
    {
        var row = (await Report(UserRole.Owner).GetStockBalanceAsync()).Single(r => r.Code == "A1");

        row.CostPrice.Should().Be(30m);
        row.StockValue.Should().Be(300m);
    }

    [Fact]
    public async Task A_service_item_has_a_null_balance_and_is_never_low_stock()
    {
        var row = (await Report(UserRole.Owner).GetStockBalanceAsync()).Single(r => r.Code == "SVC-1");

        row.CurrentStock.Should().BeNull();
        row.IsLowStock.Should().BeFalse();
    }
}
