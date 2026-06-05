using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Moq;
using MRMS.Application.DTOs;
using MRMS.Application.Services;
using MRMS.Core.Entities;
using MRMS.Core.Interfaces;

namespace MRMS.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        var passwordHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes("admin123")));

        var users = new List<User>
        {
            new() { Id = 1, Username = "tech", PasswordHash = passwordHash }
        };

        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Users.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<User, bool>> predicate) =>
                users.AsQueryable().Where(predicate).ToList());

        var service = new AuthService(mockUow.Object);
        var result = await service.LoginAsync(new LoginDto("tech", "admin123"));

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        var users = new List<User>
        {
            new() { Id = 1, Username = "tech", PasswordHash = "wronghash" }
        };

        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Users.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<User, bool>> predicate) =>
                users.AsQueryable().Where(predicate).ToList());

        var service = new AuthService(mockUow.Object);
        var result = await service.LoginAsync(new LoginDto("tech", "wrongpassword"));

        result.Success.Should().BeFalse();
    }
}
