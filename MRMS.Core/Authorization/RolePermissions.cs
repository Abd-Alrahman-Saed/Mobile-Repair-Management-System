using MRMS.Core.Enums;

namespace MRMS.Core.Authorization;

/// <summary>
/// The single source of "who can do what". Derived from the permission matrix in ERP_UPGRADE_PLAN.md.
/// <see cref="UserRole.Owner"/> is special-cased to hold every permission; the other three roles carry
/// an explicit set. Anything an Owner-only capability (ManageUsers, Backup, ViewAuditLog,
/// VoidTransaction, SellBelowCost, ApplyDiscountBeyondLimit, DeleteEntities) is simply absent from the
/// other sets.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlySet<Permission> ManagerPermissions = new HashSet<Permission>
    {
        Permission.ViewCostPrice, Permission.ViewProfit, Permission.ViewSupplierBalance,
        Permission.ViewReports, Permission.ViewCashRegister,
        Permission.ManageSuppliers, Permission.ManagePurchases, Permission.ManageItemPrices,
        Permission.ManageExpenses, Permission.ManageSettings, Permission.StockCount,
        Permission.ReceiveDevice, Permission.UpdateTicketStatus, Permission.PriceRepair,
        Permission.CollectPayment, Permission.DeductParts,
    };

    private static readonly IReadOnlySet<Permission> ReceptionistPermissions = new HashSet<Permission>
    {
        Permission.ViewCashRegister, Permission.ReceiveDevice,
        Permission.UpdateTicketStatus, Permission.CollectPayment,
    };

    private static readonly IReadOnlySet<Permission> TechnicianPermissions = new HashSet<Permission>
    {
        Permission.UpdateTicketStatus, Permission.DeductParts,
    };

    private static readonly IReadOnlyDictionary<UserRole, IReadOnlySet<Permission>> Map =
        new Dictionary<UserRole, IReadOnlySet<Permission>>
        {
            [UserRole.Manager] = ManagerPermissions,
            [UserRole.Receptionist] = ReceptionistPermissions,
            [UserRole.Technician] = TechnicianPermissions,
        };

    public static bool Has(UserRole role, Permission permission) =>
        role == UserRole.Owner || (Map.TryGetValue(role, out var set) && set.Contains(permission));

    public static IReadOnlySet<Permission> For(UserRole role) =>
        role == UserRole.Owner
            ? new HashSet<Permission>(Enum.GetValues<Permission>())
            : Map.TryGetValue(role, out var set) ? set : new HashSet<Permission>();
}
