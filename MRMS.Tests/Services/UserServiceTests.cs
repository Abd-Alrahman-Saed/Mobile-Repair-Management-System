using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Application.Security;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Infrastructure.Data;
using MRMS.Infrastructure.Repositories;

namespace MRMS.Tests.Services;

public sealed class UserServiceTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public UserServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        ctx.Users.Add(new User
        {
            Username = "owner", FullName = "المالك", Role = UserRole.Owner, IsActive = true,
            PasswordHash = "x", PasswordSalt = "y"
        });
        ctx.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private UserService NewService(UserRole actingRole = UserRole.Owner)
    {
        var uow = new UnitOfWork(new AppDbContext(_options));
        var current = FakeCurrentUserService.As(actingRole);
        var perms = new PermissionService(current);
        return new UserService(uow, perms, current, new AuditService(uow, current, perms));
    }

    [Fact]
    public async Task CreateAsync_hashes_password_and_forces_change()
    {
        var created = await NewService().CreateAsync(
            new CreateUserDto("reception1", "منى", UserRole.Receptionist, "pass1"));

        created.Username.Should().Be("reception1");
        created.Role.Should().Be(UserRole.Receptionist);

        await using var ctx = new AppDbContext(_options);
        var row = await ctx.Users.SingleAsync(u => u.Username == "reception1");
        row.MustChangePassword.Should().BeTrue();
        row.PasswordSalt.Should().NotBeNullOrEmpty();
        PasswordHasher.VerifyPbkdf2("pass1", row.PasswordHash, row.PasswordSalt!).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_username()
    {
        var act = () => NewService().CreateAsync(new CreateUserDto("owner", "x", UserRole.Manager, "pass1"));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_cannot_demote_the_last_owner()
    {
        int ownerId;
        await using (var ctx = new AppDbContext(_options))
            ownerId = (await ctx.Users.SingleAsync(u => u.Role == UserRole.Owner)).Id;

        var act = () => NewService().UpdateAsync(new UpdateUserDto(ownerId, "المالك", UserRole.Manager, true));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_cannot_deactivate_the_last_owner()
    {
        int ownerId;
        await using (var ctx = new AppDbContext(_options))
            ownerId = (await ctx.Users.SingleAsync(u => u.Role == UserRole.Owner)).Id;

        var act = () => NewService().UpdateAsync(new UpdateUserDto(ownerId, "المالك", UserRole.Owner, false));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Every_method_requires_ManageUsers()
    {
        var reception = NewService(UserRole.Receptionist);
        await FluentActions.Awaiting(() => reception.GetAllAsync())
            .Should().ThrowAsync<UnauthorizedAccessException>();
        await FluentActions.Awaiting(() => reception.CreateAsync(new CreateUserDto("a", "b", UserRole.Technician, "pass1")))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAsync_writes_an_audit_row()
    {
        await NewService().CreateAsync(new CreateUserDto("tech1", "علي", UserRole.Technician, "pass1"));

        await using var ctx = new AppDbContext(_options);
        (await ctx.AuditLogs.AnyAsync(a => a.Action == "User.Create")).Should().BeTrue();
    }
}
