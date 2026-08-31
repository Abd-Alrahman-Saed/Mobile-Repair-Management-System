using System.Globalization;
using System.Windows.Data;
using MRMS.Core.Enums;

namespace MRMS.UI.Converters;

public class TicketStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TicketStatus status)
        {
            return status switch
            {
                TicketStatus.Received => "مستلم",
                TicketStatus.UnderInspection => "قيد الفحص",
                TicketStatus.WaitingForParts => "بانتظار القطع",
                TicketStatus.InRepair => "قيد الصيانة",
                TicketStatus.Repaired => "تمت الصيانة",
                TicketStatus.Delivered => "تم التسليم",
                TicketStatus.Cancelled => "ملغي",
                _ => status.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
