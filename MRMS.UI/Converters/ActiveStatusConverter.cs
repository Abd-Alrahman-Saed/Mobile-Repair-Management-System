using System.Globalization;
using System.Windows.Data;

namespace MRMS.UI.Converters;

/// <summary>true -> "نشط", false -> "موقوف". For the users grid status column.</summary>
public class ActiveStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "نشط" : "موقوف";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
