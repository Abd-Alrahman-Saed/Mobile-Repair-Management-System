using MRMS.Application.Interfaces;
using MRMS.Core.Authorization;
using MRMS.Core.Enums;

namespace MRMS.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly ICurrentUserService _currentUser;

    public PermissionService(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public bool Can(Permission permission) =>
        _currentUser.Role is { } role && RolePermissions.Has(role, permission);

    public void Require(Permission permission)
    {
        if (!Can(permission))
            throw new UnauthorizedAccessException("ليس لديك صلاحية لهذا الإجراء");
    }
}
