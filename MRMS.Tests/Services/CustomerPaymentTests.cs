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

/// <summary>Phase 6.2 — customer collections (mirrors SupplierPayment) and the receivables report
/// that drives the customer list's "مديونية" column.</summary>
public sealed class CustomerPaymentTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _itemId;
    private int _customerId;

    public CustomerPaymentTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        var item = new Item { Code = "A1", Name = "جراب", CostPrice = 30, SellingPrice = 50, CurrentStock = 100 };
        ctx.Items.Add(item);
        var customer = new Customer { FullName = "عميل آجل", PhoneNumber = "0100" };
        ctx.Customers.Add(customer);
        ctx.SaveChanges();

        _itemId = item.Id;
        _customerId = customer.Id;
    }

    public void Dispose() => _connection.Dispose();

    private IUnitOfWork Uow() => new UnitOfWork(new AppDbContext(_options));

    private SaleService Sales()
    {
        var uow = Uow();
        var current = FakeCurrentUserService.As(UserRole.Owner);
        var perms = TestServices.Permissions(current);
        return new SaleService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, current, new SystemSettingService(uow, perms), new FakeCashSessionService());
    }

    private CustomerPaymentService Payments()
    {
        var uow = Uow();
        var current = FakeCurrentUserService.As(UserRole.Owner);
        var perms = TestServices.Permissions(current);
        return new CustomerPaymentService(uow, new JournalService(uow, perms), new AuditService(uow, current, perms),
            perms, current, new FakeCashSessionService());
    }

    private ReportService Report() => new(Uow(), TestServices.Permissions(UserRole.Owner));

    [Fact]
    public async Task Collecting_a_payment_reduces_the_customers_outstanding_balance()
    {
        await Sales().CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, _customerId, "", PaymentMethod.Credit,
            [new CreateSalesInvoiceLineDto(_itemId, 4, 50m)], 0, 0m, ""));  // Total 200, unpaid

        var before = (await Report().GetReceivablesAsync()).Single(r => r.CustomerId == _customerId);
        before.OutstandingBalance.Should().Be(200m);

        await Payments().CreateAsync(new CreateCustomerPaymentDto(DateTime.Today, _customerId, 120m, "ref", ""));

        var after = (await Report().GetReceivablesAsync()).Single(r => r.CustomerId == _customerId);
        after.OutstandingBalance.Should().Be(80m);
        after.TotalCollected.Should().Be(120m);
    }

    [Fact]
    public async Task A_deposit_taken_at_sale_time_already_counts_toward_collected()
    {
        // Total 200, 50 collected as a deposit at sale time - no separate CustomerPayment needed for that part.
        await Sales().CreateAsync(new CreateSalesInvoiceDto(
            DateTime.Today, _customerId, "", PaymentMethod.Credit,
            [new CreateSalesInvoiceLineDto(_itemId, 4, 50m)], 0, 50m, ""));

        var receivable = (await Report().GetReceivablesAsync()).Single(r => r.CustomerId == _customerId);
        receivable.TotalInvoiced.Should().Be(200m);
        receivable.TotalCollected.Should().Be(50m);
        receivable.OutstandingBalance.Should().Be(150m);
    }

    [Fact]
    public async Task Collecting_a_non_positive_amount_is_rejected()
    {
        await FluentActions.Awaiting(() => Payments().CreateAsync(new CreateCustomerPaymentDto(DateTime.Today, _customerId, 0m, "", "")))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
