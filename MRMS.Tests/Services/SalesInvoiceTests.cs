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

/// <summary>Phase 6.1/6.2 — multi-line sales invoices replacing the old single-item Sale: correct
/// totals, per-line cost locking, credit-sale customer requirement, and void behaviour.</summary>
public sealed class SalesInvoiceTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _itemAId;
    private int _itemBId;
    private int _customerId;

    public SalesInvoiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        var itemA = new Item { Code = "A1", Name = "جراب", CostPrice = 30, SellingPrice = 50, CurrentStock = 10 };
        var itemB = new Item { Code = "A2", Name = "شاحن", CostPrice = 20, SellingPrice = 40, CurrentStock = 5 };
        ctx.Items.AddRange(itemA, itemB);

        var customer = new Customer { FullName = "عميل مسجل", PhoneNumber = "0100" };
        ctx.Customers.Add(customer);
        ctx.SaveChanges();

        _itemAId = itemA.Id;
        _itemBId = itemB.Id;
        _customerId = customer.Id;
    }

    public void Dispose() => _connection.Dispose();

    private IUnitOfWork Uow() => new UnitOfWork(new AppDbContext(_options));

    private SaleService Sales(UserRole role = UserRole.Owner)
    {
        var uow = Uow();
        var current = FakeCurrentUserService.As(role);
        var perms = TestServices.Permissions(current);
        return new SaleService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, current, new SystemSettingService(uow, perms), new FakeCashSessionService());
    }

    [Fact]
    public async Task A_multi_line_invoice_gets_one_number_and_the_right_total_and_stock()
    {
        var invoice = await Sales().CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, null, "زبون سريع", PaymentMethod.Cash,
            Lines:
            [
                new CreateSalesInvoiceLineDto(_itemAId, 2, 50m),  // 100
                new CreateSalesInvoiceLineDto(_itemBId, 1, 40m),  // 40
            ],
            DiscountAmount: 10m, PaidAmount: 130m, Notes: ""));

        invoice.InvoiceNumber.Should().StartWith("INV-");
        invoice.Subtotal.Should().Be(140m);
        invoice.Total.Should().Be(130m);
        invoice.Lines.Should().HaveCount(2);

        await using var ctx = new AppDbContext(_options);
        (await ctx.Items.FindAsync(_itemAId))!.CurrentStock.Should().Be(8);
        (await ctx.Items.FindAsync(_itemBId))!.CurrentStock.Should().Be(4);
    }

    [Fact]
    public async Task UnitCost_is_locked_from_the_item_not_the_caller()
    {
        var invoice = await Sales(UserRole.Receptionist).CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, null, "x", PaymentMethod.Cash,
            [new CreateSalesInvoiceLineDto(_itemAId, 1, 50m)], 0, 50m, ""));

        await using var ctx = new AppDbContext(_options);
        (await ctx.SalesInvoiceLines.SingleAsync(l => l.SalesInvoiceId == invoice.Id))
            .UnitCost.Should().Be(30m);
    }

    [Fact]
    public async Task A_credit_sale_without_a_registered_customer_is_rejected()
    {
        var act = () => Sales().CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, null, "بدون تسجيل", PaymentMethod.Credit,
            [new CreateSalesInvoiceLineDto(_itemAId, 1, 50m)], 0, 0m, ""));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task A_credit_sale_with_a_registered_customer_succeeds_with_a_remaining_balance()
    {
        var invoice = await Sales().CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, _customerId, "", PaymentMethod.Credit,
            [new CreateSalesInvoiceLineDto(_itemAId, 2, 50m)], 0, 30m, ""));

        invoice.Total.Should().Be(100m);
        invoice.PaidAmount.Should().Be(30m);
        invoice.RemainingAmount.Should().Be(70m);

        await using var ctx = new AppDbContext(_options);
        var lines = await ctx.JournalEntryLines.ToListAsync();
        lines.Sum(l => l.DebitAmount).Should().Be(30m, "only the deposit is posted, not the full 100 total");
    }

    [Fact]
    public async Task Voiding_an_invoice_restores_stock_for_every_line_and_posts_a_reversal()
    {
        var invoice = await Sales().CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, null, "x", PaymentMethod.Cash,
            [
                new CreateSalesInvoiceLineDto(_itemAId, 2, 50m),
                new CreateSalesInvoiceLineDto(_itemBId, 1, 40m),
            ], 0, 140m, ""));

        await Sales().VoidAsync(invoice.Id, "خطأ في الفاتورة");

        await using var ctx = new AppDbContext(_options);
        (await ctx.Items.FindAsync(_itemAId))!.CurrentStock.Should().Be(10, "back to the original stock");
        (await ctx.Items.FindAsync(_itemBId))!.CurrentStock.Should().Be(5);

        var lines = await ctx.JournalEntryLines.ToListAsync();
        lines.Sum(l => l.DebitAmount).Should().Be(lines.Sum(l => l.CreditAmount), "original + reversal net out");
        (await ctx.SalesInvoices.IgnoreQueryFilters().SingleAsync(i => i.Id == invoice.Id)).IsVoided.Should().BeTrue();
    }
}
