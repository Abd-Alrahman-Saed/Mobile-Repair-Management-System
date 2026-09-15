using System.Globalization;
using System.Windows.Data;

namespace MRMS.UI.Converters;

public class LessThanZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value switch { decimal d => d < 0, int i => i < 0, _ => false };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
