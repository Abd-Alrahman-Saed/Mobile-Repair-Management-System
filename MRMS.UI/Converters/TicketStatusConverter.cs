using System.Globalization;
using System.Windows.Data;
using MRMS.Core.Enums;
using MRMS.UI.Resources;

namespace MRMS.UI.Converters;

/// <summary>Turns a <see cref="TicketStatus"/> into its Arabic label from <see cref="TicketStatusPalette"/>.</summary>
public class TicketStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is TicketStatus status ? TicketStatusPalette.Label(status) : value?.ToString() ?? "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
