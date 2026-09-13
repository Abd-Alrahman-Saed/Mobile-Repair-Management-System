using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Infrastructure.Data;
using MRMS.Infrastructure.Repositories;

namespace MRMS.Tests.Services;

/// <summary>Phase 2.5 - discount ceilings by role and the below-cost block, overridable by an owner approval.</summary>
public sealed class DiscountLimitTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private int _itemId;
    private int _ownerId;

    public DiscountLimitTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        var item = new Item { Code = "A1", Name = "جراب", CostPrice = 40, SellingPrice = 100, CurrentStock = 20 };
        ctx.Items.Add(item);
        var owner = new User { Username = "owner", FullName = "المالك", Role = UserRole.Owner, IsActive = true, PasswordHash = "x", PasswordSalt = "y" };
        ctx.Users.Add(owner);
        ctx.SystemSettings.Add(new SystemSetting { Key = "Discount.MaxPercent.Receptionist", Value = "10" });
        ctx.SaveChanges();
        _itemId = item.Id;
        _ownerId = owner.Id;
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

    private CreateSalesInvoiceDto Dto(decimal unitPrice, int? approvedBy = null) =>
        new(DateTime.Today, CustomerId: null, CustomerName: "x", PaymentMethod.Cash,
            Lines: [new CreateSalesInvoiceLineDto(_itemId, 1, unitPrice)],
            DiscountAmount: 0, PaidAmount: unitPrice, Notes: "", ApprovedByUserId: approvedBy);

    [Fact]
    public async Task Receptionist_within_the_ceiling_is_allowed()
    {
        await FluentActions.Awaiting(() => Sales(UserRole.Receptionist).CreateAsync(Dto(92)))  // 8% discount
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task Receptionist_over_the_ceiling_is_rejected()
    {
        await FluentActions.Awaiting(() => Sales(UserRole.Receptionist).CreateAsync(Dto(80)))  // 20% discount
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*موافقة المالك*");
    }

    [Fact]
    public async Task Selling_below_cost_is_rejected_for_a_receptionist()
    {
        await FluentActions.Awaiting(() => Sales(UserRole.Receptionist).CreateAsync(Dto(35)))  // below cost 40
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*أقل من التكلفة*");
    }

    [Fact]
    public async Task Owner_has_no_discount_ceiling()
    {
        await FluentActions.Awaiting(() => Sales(UserRole.Owner).CreateAsync(Dto(10)))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task A_valid_owner_approval_lets_a_receptionist_exceed_the_ceiling()
    {
        var sale = await Sales(UserRole.Receptionist).CreateAsync(Dto(50, approvedBy: _ownerId));
        sale.Id.Should().BeGreaterThan(0);

        await using var ctx = new AppDbContext(_options);
        (await ctx.AuditLogs.AnyAsync(a => a.Action == "SalesInvoice.OverrideApproved")).Should().BeTrue();
    }

    [Fact]
    public async Task An_approval_by_a_non_authorised_user_is_ignored()
    {
        int receptionId;
        await using (var ctx = new AppDbContext(_options))
        {
            var r = new User { Username = "r", FullName = "r", Role = UserRole.Receptionist, IsActive = true, PasswordHash = "x", PasswordSalt = "y" };
            ctx.Users.Add(r);
            await ctx.SaveChangesAsync();
            receptionId = r.Id;
        }

        await FluentActions.Awaiting(() => Sales(UserRole.Receptionist).CreateAsync(Dto(50, approvedBy: receptionId)))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
