using System.Globalization;
using System.Windows.Data;

namespace MRMS.UI.Converters;

public class InverseSignConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d ? -d : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d ? -d : value;
}
