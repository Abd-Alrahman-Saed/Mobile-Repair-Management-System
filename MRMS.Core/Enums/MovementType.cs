namespace MRMS.Core.Enums;

public enum MovementType
{
    CustomerReturn,
    SupplierReturn,
    Damaged,
    /// <summary>Stock count (جرد) found more than the book balance.</summary>
    AdjustmentIncrease,
    /// <summary>Stock count (جرد) found less than the book balance.</summary>
    AdjustmentDecrease
}
