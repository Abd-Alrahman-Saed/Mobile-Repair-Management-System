using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MRMS.UI.Converters;

/// <summary>Colour to go with <see cref="StockStatusConverter"/>'s label: green/available, yellow/low,
/// red/out-of-stock, gray for a Service item ("—").</summary>
public class StockStatusBrushConverter : IMultiValueConverter
{
    private static readonly SolidColorBrush Available = Freeze("#16A34A");
    private static readonly SolidColorBrush Low = Freeze("#B45309");
    private static readonly SolidColorBrush OutOfStock = Freeze("#DC2626");
    private static readonly SolidColorBrush NotTracked = Freeze("#98989B");

    private static SolidColorBrush Freeze(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not int stock)
            return NotTracked;

        var reorderLevel = values[1] is int r ? r : 0;

        if (stock <= 0) return OutOfStock;
        if (stock <= reorderLevel) return Low;
        return Available;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
