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

public sealed class AuditQueryTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AuditQueryTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        ctx.AuditLogs.AddRange(
            new AuditLog { UserName = "المالك", Action = "Sale.Create", EntityType = "Sale", CreatedAt = new DateTime(2026, 1, 10) },
            new AuditLog { UserName = "منى", Action = "Expense.Create", EntityType = "Expense", CreatedAt = new DateTime(2026, 2, 15) },
            new AuditLog { UserName = "منى", Action = "Sale.Create", EntityType = "Sale", CreatedAt = new DateTime(2026, 3, 1) });
        ctx.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private AuditService NewService(UserRole role = UserRole.Owner)
    {
        var uow = new UnitOfWork(new AppDbContext(_options));
        var current = FakeCurrentUserService.As(role);
        return new AuditService(uow, current, new PermissionService(current));
    }

    [Fact]
    public async Task QueryAsync_filters_by_action_and_user_and_date()
    {
        var svc = NewService();

        (await svc.QueryAsync(action: "Sale.Create")).Should().HaveCount(2);
        (await svc.QueryAsync(userName: "منى")).Should().HaveCount(2);
        (await svc.QueryAsync(from: new DateTime(2026, 2, 1))).Should().HaveCount(2);
        (await svc.QueryAsync(from: new DateTime(2026, 2, 1), to: new DateTime(2026, 2, 28), action: "Expense.Create"))
            .Should().ContainSingle();
    }

    [Fact]
    public async Task QueryAsync_requires_ViewAuditLog()
    {
        await FluentActions.Awaiting(() => NewService(UserRole.Manager).QueryAsync())
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
