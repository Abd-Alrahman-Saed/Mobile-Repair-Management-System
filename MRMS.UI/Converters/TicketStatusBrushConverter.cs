using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MRMS.Core.Enums;
using MRMS.UI.Resources;

namespace MRMS.UI.Converters;

/// <summary>
/// Turns a <see cref="TicketStatus"/> into one of its colours from <see cref="TicketStatusPalette"/>.
/// ConverterParameter selects which: "Background" (chip fill, default), "Text" (chip text),
/// "Column" (Kanban column header). Replaces the per-label DataTrigger chains that used to live in
/// DashboardPage / CustomerProfilePage / RepairTicketDetailPage.
/// </summary>
public class TicketStatusBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TicketStatus status)
            return Brushes.Transparent;

        return (parameter as string) switch
        {
            "Text" => TicketStatusPalette.TextBrush(status),
            "Column" => TicketStatusPalette.ColumnBrush(status),
            _ => TicketStatusPalette.BackgroundBrush(status),
        };
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
