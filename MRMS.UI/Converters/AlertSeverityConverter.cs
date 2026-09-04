using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MRMS.UI.Converters;

/// <summary>"danger" / "warning" -> a dot brush for the alerts dropdown.</summary>
public class AlertSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (value as string) switch
        {
            "danger" => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),
            _ => new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06)),
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
