using MRMS.Core.Enums;

namespace MRMS.Application.Interfaces;

public interface IPermissionService
{
    bool Can(Permission permission);

    /// <summary>Throws <see cref="UnauthorizedAccessException"/> if the current user lacks <paramref name="permission"/>.</summary>
    void Require(Permission permission);
}
