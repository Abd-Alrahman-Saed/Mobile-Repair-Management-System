using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MRMS.UI.Converters;

/// <summary>Non-empty string -> Visible; null/empty/whitespace -> Collapsed. The inverse of
/// <see cref="EmptyStringToVisibilityConverter"/>, for content that should only show up when present
/// (e.g. an assigned technician's name).</summary>
public class NonEmptyStringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
