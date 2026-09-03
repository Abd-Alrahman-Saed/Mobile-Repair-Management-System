namespace MRMS.Core.Enums;

/// <summary>
/// The four roles a shop account can have. Owner sees and can do everything; the rest are scoped by
/// <see cref="Permission"/> via <see cref="MRMS.Core.Authorization.RolePermissions"/>.
/// </summary>
public enum UserRole
{
    Owner,
    Manager,
    Receptionist,
    Technician
}
