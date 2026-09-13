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

/// <summary>Phase 6 follow-up — MonthlyReportService's cash-flow figures must reflect what was
/// actually collected (invoice PaidAmount + CustomerPayment), not accrual revenue (invoice Total),
/// the same cash-basis fix already applied to the cashbook/dashboard in Phase 5.</summary>
public sealed class MonthlyReportCashFlowTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _itemId;
    private int _customerId;

    public MonthlyReportCashFlowTests()
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

    private MonthlyReportService Report() => new(Uow(), TestServices.Permissions(UserRole.Owner));

    [Fact]
    public async Task A_credit_sale_counts_its_full_total_as_revenue_but_only_the_deposit_as_cash_in()
    {
        var today = DateTime.Today;
        await Sales().CreateAsync(new CreateSalesInvoiceDto(
            today, _customerId, "", PaymentMethod.Credit,
            [new CreateSalesInvoiceLineDto(_itemId, 4, 50m)], 0, 60m, ""));  // Total 200, only 60 collected

        var report = await Report().GetMonthlyReportAsync(today.Year, today.Month);
        var row = report.Rows.Single(r => r.Month == today.Month);

        row.SalesRevenue.Should().Be(200m, "revenue stays accrual-basis, consistent with IncomeStatementService");
        row.NetCashFlow.Should().Be(60m, "cash flow reflects only the deposit actually collected");
    }
}
