using System.Windows.Media;
using MRMS.Core.Enums;

namespace MRMS.UI.Resources;

/// <summary>
/// The one place that maps a <see cref="TicketStatus"/> to its Arabic label and its three colours
/// (chip background, chip text, Kanban column header). Values are taken verbatim from the redesign.
/// Everything that shows a status - the converters, the Kanban groups, the detail screen, the printed
/// receipt, the status-change toast - reads from here, so the wording and colours can never drift apart
/// again (they used to be copied into four places, two of which said "تحت الفحص" instead of "قيد الفحص").
/// </summary>
public static class TicketStatusPalette
{
    public readonly record struct Entry(string Label, Color Background, Color Text, Color Column);

    private static readonly IReadOnlyDictionary<TicketStatus, Entry> Map = new Dictionary<TicketStatus, Entry>
    {
        [TicketStatus.Received]         = new("مستلم",          Hex("#F5F5F8"), Hex("#424244"), Hex("#98989B")),
        [TicketStatus.UnderInspection]  = new("قيد الفحص",      Hex("#DBEAFE"), Hex("#1E4E79"), Hex("#3B8BC2")),
        [TicketStatus.WaitingForParts]  = new("بانتظار القطع",  Hex("#FEF3C7"), Hex("#B45309"), Hex("#D97706")),
        [TicketStatus.InRepair]         = new("قيد الإصلاح",    Hex("#BFDCF0"), Hex("#124467"), Hex("#1D6FA5")),
        [TicketStatus.Repaired]         = new("تم الإصلاح",     Hex("#DCFCE7"), Hex("#16A34A"), Hex("#22C55E")),
        [TicketStatus.Delivered]        = new("تم التسليم",     Hex("#BBF7D0"), Hex("#166534"), Hex("#16A34A")),
        [TicketStatus.Cancelled]        = new("ملغي",           Hex("#FEE2E2"), Hex("#DC2626"), Hex("#DC2626")),
    };

    private static readonly Dictionary<TicketStatus, SolidColorBrush> BackgroundBrushes = BrushCache(e => e.Background);
    private static readonly Dictionary<TicketStatus, SolidColorBrush> TextBrushes = BrushCache(e => e.Text);
    private static readonly Dictionary<TicketStatus, SolidColorBrush> ColumnBrushes = BrushCache(e => e.Column);

    public static IEnumerable<TicketStatus> Ordered => Map.Keys;

    public static string Label(TicketStatus status) =>
        Map.TryGetValue(status, out var e) ? e.Label : status.ToString();

    public static SolidColorBrush BackgroundBrush(TicketStatus status) => BackgroundBrushes[status];
    public static SolidColorBrush TextBrush(TicketStatus status) => TextBrushes[status];
    public static SolidColorBrush ColumnBrush(TicketStatus status) => ColumnBrushes[status];

    private static Dictionary<TicketStatus, SolidColorBrush> BrushCache(Func<Entry, Color> pick)
    {
        var cache = new Dictionary<TicketStatus, SolidColorBrush>();
        foreach (var (status, entry) in Map)
        {
            var brush = new SolidColorBrush(pick(entry));
            brush.Freeze();
            cache[status] = brush;
        }
        return cache;
    }

    private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
