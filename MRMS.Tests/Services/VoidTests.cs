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

/// <summary>Phase 2.6 - voiding keeps the row, restores stock, posts a reversing entry, and drops out of reports.</summary>
public sealed class VoidTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _itemId;

    public VoidTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        var item = new Item { Code = "A1", Name = "جراب", CostPrice = 30, SellingPrice = 50, CurrentStock = 10 };
        ctx.Items.Add(item);
        ctx.SaveChanges();
        _itemId = item.Id;
    }

    public void Dispose() => _connection.Dispose();

    private SaleService Sales(UserRole role)
    {
        var uow = new UnitOfWork(new AppDbContext(_options));
        var current = FakeCurrentUserService.As(role);
        var perms = new PermissionService(current);
        return new SaleService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, current, new SystemSettingService(uow, perms), new FakeCashSessionService());
    }

    private async Task<int> CreateSaleAsync()
    {
        var sale = await Sales(UserRole.Owner).CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, CustomerId: null, CustomerName: "x", PaymentMethod.Cash,
            Lines: [new CreateSalesInvoiceLineDto(_itemId, 4, 50)],
            DiscountAmount: 0, PaidAmount: 200, Notes: ""));
        return sale.Id;
    }

    [Fact]
    public async Task Void_keeps_the_row_restores_stock_and_posts_a_balanced_reversal()
    {
        var saleId = await CreateSaleAsync();

        await Sales(UserRole.Owner).VoidAsync(saleId, "إدخال خاطئ");

        await using var ctx = new AppDbContext(_options);
        var sale = await ctx.SalesInvoices.IgnoreQueryFilters().SingleAsync(s => s.Id == saleId);
        sale.IsVoided.Should().BeTrue();
        sale.VoidReason.Should().Be("إدخال خاطئ");

        (await ctx.Items.FindAsync(_itemId))!.CurrentStock.Should().Be(10, "the 4 units are back");

        var lines = await ctx.JournalEntryLines.ToListAsync();
        lines.Sum(l => l.DebitAmount).Should().Be(lines.Sum(l => l.CreditAmount), "original + reversal net out");
        (await ctx.JournalEntries.AnyAsync(e => e.ReferenceType == "SalesInvoice.Reversal")).Should().BeTrue();
        (await ctx.AuditLogs.AnyAsync(a => a.Action == "SalesInvoice.Void")).Should().BeTrue();
    }

    [Fact]
    public async Task A_voided_sale_is_hidden_from_reports_and_from_non_owner_lists()
    {
        var saleId = await CreateSaleAsync();
        await Sales(UserRole.Owner).VoidAsync(saleId, "test");

        // Global query filter: a plain query sees nothing.
        await using var ctx = new AppDbContext(_options);
        (await ctx.SalesInvoices.AnyAsync()).Should().BeFalse();

        // Owner list ignores the filter and still shows it (struck through in the UI).
        (await Sales(UserRole.Owner).GetAllAsync()).Should().ContainSingle(s => s.IsVoided);
        // Receptionist list does not.
        (await Sales(UserRole.Receptionist).GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Voiding_twice_is_rejected()
    {
        var saleId = await CreateSaleAsync();
        await Sales(UserRole.Owner).VoidAsync(saleId, "first");

        await FluentActions.Awaiting(() => Sales(UserRole.Owner).VoidAsync(saleId, "again"))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Void_requires_VoidTransaction_permission()
    {
        var saleId = await CreateSaleAsync();
        await FluentActions.Awaiting(() => Sales(UserRole.Manager).VoidAsync(saleId, "x"))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
