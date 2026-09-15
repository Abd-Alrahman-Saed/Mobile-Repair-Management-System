namespace MRMS.Core.Enums;

public enum ItemType
{
    Accessory,
    SparePart,
    /// <summary>A labor/service line (e.g. "خدمة تركيب شاشة") - never has stock: no CurrentStock,
    /// excluded from stock-value/low-stock calculations, and never blocked for insufficient stock.</summary>
    Service
}
