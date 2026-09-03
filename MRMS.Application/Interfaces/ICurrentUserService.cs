using MRMS.Core.Enums;

namespace MRMS.Application.Interfaces;

public record CurrentUser(int Id, string Username, string FullName, UserRole Role);

/// <summary>
/// Holds the signed-in user for the life of the process (single-user desktop app). Populated by
/// <see cref="IAuthService.LoginAsync"/>, read by <c>PermissionService</c> and <c>AuditService</c>,
/// cleared on logout / shutdown.
/// </summary>
public interface ICurrentUserService
{
    CurrentUser? User { get; }
    bool IsAuthenticated { get; }
    UserRole? Role { get; }

    void SetCurrentUser(CurrentUser user);
    void Clear();
}
