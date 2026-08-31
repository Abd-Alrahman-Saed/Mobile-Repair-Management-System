using System.Globalization;
using System.Windows.Data;

namespace MRMS.UI.Converters;

public class BoolToResultTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "نجاح" : "فشل";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
