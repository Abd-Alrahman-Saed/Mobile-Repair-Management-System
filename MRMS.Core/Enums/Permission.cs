namespace MRMS.Core.Enums;

/// <summary>
/// Fine-grained capabilities checked by the service layer. A <see cref="UserRole"/> maps to a set of
/// these in <see cref="MRMS.Core.Authorization.RolePermissions"/>. Hiding a button is not protection -
/// the service method itself calls <c>PermissionService.Require(...)</c>.
/// </summary>
public enum Permission
{
    // Visibility of money the shop floor should not see
    ViewCostPrice,
    ViewProfit,
    ViewSupplierBalance,
    ViewReports,
    ViewCashRegister,
    ViewAuditLog,

    // Management
    ManageSuppliers,
    ManagePurchases,
    ManageItemPrices,
    ManageExpenses,
    ManageUsers,
    ManageSettings,
    StockCount,
    Backup,

    // Repair workflow
    ReceiveDevice,
    UpdateTicketStatus,
    PriceRepair,
    CollectPayment,
    DeductParts,

    // Elevated / owner-guarded
    ApplyDiscountBeyondLimit,
    SellBelowCost,
    VoidTransaction,
    DeleteEntities
}
