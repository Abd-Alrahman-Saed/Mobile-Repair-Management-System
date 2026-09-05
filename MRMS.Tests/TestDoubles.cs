using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.Application.Services;
using MRMS.Core.Enums;

namespace MRMS.Tests;

/// <summary>A fixed signed-in user for service tests. Defaults to an Owner (can do everything).</summary>
public sealed class FakeCurrentUserService : ICurrentUserService
{
    public CurrentUser? User { get; private set; } =
        new(1, "owner", "المالك", UserRole.Owner);

    public bool IsAuthenticated => User is not null;
    public UserRole? Role => User?.Role;
    public void SetCurrentUser(CurrentUser user) => User = user;
    public void Clear() => User = null;

    public static FakeCurrentUserService As(UserRole role) =>
        new() { User = new CurrentUser(1, role.ToString().ToLowerInvariant(), role.ToString(), role) };
}

/// <summary>No cash session ever open, nothing ever locked. For service tests that aren't about the
/// cash drawer. Cash-drawer behaviour is covered against the real service in CashSessionTests.</summary>
public sealed class FakeCashSessionService : ICashSessionService
{
    public Task<CashSessionDto?> GetCurrentAsync() => Task.FromResult<CashSessionDto?>(null);
    public Task<int?> GetCurrentSessionIdAsync() => Task.FromResult<int?>(null);
    public Task EnsureNotLockedAsync(DateTime businessDate) => Task.CompletedTask;
    public Task<CashSessionDto> OpenAsync(decimal openingBalance) => throw new NotSupportedException();
    public Task<CashSessionDto> CloseAsync(decimal countedAmount, string notes) => throw new NotSupportedException();
    public Task<IEnumerable<CashSessionDto>> GetHistoryAsync() => throw new NotSupportedException();
}

public static class TestServices
{
    public static IPermissionService Permissions(UserRole role = UserRole.Owner) =>
        new PermissionService(FakeCurrentUserService.As(role));

    public static IPermissionService Permissions(ICurrentUserService currentUser) =>
        new PermissionService(currentUser);

    public static ICashSessionService CashSessions() => new FakeCashSessionService();
}
