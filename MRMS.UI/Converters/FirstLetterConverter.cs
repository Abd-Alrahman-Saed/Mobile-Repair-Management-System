using System.Globalization;
using System.Windows.Data;

namespace MRMS.UI.Converters;

[ValueConversion(typeof(string), typeof(string))]
public class FirstLetterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && str.Length > 0)
            return str[0].ToString();
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
