using System.Globalization;
using System.Windows.Data;

namespace MRMS.UI.Converters;

/// <summary>Turns [CurrentStock (int?), ReorderLevel (int)] into an Arabic status label:
/// "نفذ من المخزون" / "منخفض" / "متوفر" / "—" for a Service item with no stock at all.</summary>
public class StockStatusConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        // A null CurrentStock (Service item) arrives as null, not a boxed Nullable<int> - a value
        // arrives boxed as plain int (nullable value types box to their underlying type when set).
        if (values.Length < 2 || values[0] is not int stock)
            return "—";

        var reorderLevel = values[1] is int r ? r : 0;

        if (stock <= 0) return "نفذ من المخزون";
        if (stock <= reorderLevel) return "منخفض";
        return "متوفر";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
