using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Moq;
using MRMS.Application.DTOs;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Enums;
using MRMS.Core.Interfaces;

namespace MRMS.Tests.Services;

public class AuthServiceTests
{
    private static (Mock<IUnitOfWork> uow, List<User> users) MockUow(params User[] seed)
    {
        var users = seed.ToList();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Users.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((Expression<Func<User, bool>> p) => users.AsQueryable().Where(p).ToList());
        uow.Setup(u => u.Users.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => users.FirstOrDefault(u => u.Id == id));
        uow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
        return (uow, users);
    }

    private static AuthService NewService(Mock<IUnitOfWork> uow, out FakeCurrentUserService current)
    {
        current = new FakeCurrentUserService();
        current.Clear();
        return new AuthService(uow.Object, current);
    }

    [Fact]
    public async Task LoginAsync_pbkdf2_credentials_succeeds_and_populates_current_user()
    {
        var (hash, salt) = MRMS.Application.Security.PasswordHasher.Hash("s3cret");
        var (uow, _) = MockUow(new User
        {
            Id = 7, Username = "sara", FullName = "سارة", Role = UserRole.Manager,
            IsActive = true, PasswordHash = hash, PasswordSalt = salt
        });
        var service = NewService(uow, out var current);

        var result = await service.LoginAsync(new LoginDto("sara", "s3cret"));

        result.Success.Should().BeTrue();
        current.User!.Id.Should().Be(7);
        current.User.Role.Should().Be(UserRole.Manager);
    }

    [Fact]
    public async Task LoginAsync_wrong_password_fails_and_leaves_current_user_empty()
    {
        var (hash, salt) = MRMS.Application.Security.PasswordHasher.Hash("right");
        var (uow, _) = MockUow(new User { Id = 1, Username = "u", IsActive = true, PasswordHash = hash, PasswordSalt = salt });
        var service = NewService(uow, out var current);

        var result = await service.LoginAsync(new LoginDto("u", "wrong"));

        result.Success.Should().BeFalse();
        current.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_inactive_user_is_rejected()
    {
        var (hash, salt) = MRMS.Application.Security.PasswordHasher.Hash("pw");
        var (uow, _) = MockUow(new User { Id = 1, Username = "u", IsActive = false, PasswordHash = hash, PasswordSalt = salt });
        var service = NewService(uow, out _);

        (await service.LoginAsync(new LoginDto("u", "pw"))).Success.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_legacy_sha256_hash_verifies_then_upgrades_to_pbkdf2()
    {
        var legacy = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("admin123")));
        var user = new User { Id = 1, Username = "tech", IsActive = true, PasswordHash = legacy, PasswordSalt = null };
        var (uow, _) = MockUow(user);
        var service = NewService(uow, out _);

        var result = await service.LoginAsync(new LoginDto("tech", "admin123"));

        result.Success.Should().BeTrue();
        user.PasswordSalt.Should().NotBeNull("the legacy hash is re-hashed on first successful login");
        user.PasswordHash.Should().NotBe(legacy);
        MRMS.Application.Security.PasswordHasher.VerifyPbkdf2("admin123", user.PasswordHash, user.PasswordSalt!)
            .Should().BeTrue();
    }
}
