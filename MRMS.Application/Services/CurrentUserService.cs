using MRMS.Application.Interfaces;
using MRMS.Core.Enums;

namespace MRMS.Application.Services;

public class CurrentUserService : ICurrentUserService
{
    public CurrentUser? User { get; private set; }

    public bool IsAuthenticated => User is not null;

    public UserRole? Role => User?.Role;

    public void SetCurrentUser(CurrentUser user) => User = user;

    public void Clear() => User = null;
}
