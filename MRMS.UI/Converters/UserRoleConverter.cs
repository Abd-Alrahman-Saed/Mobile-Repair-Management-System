using System.Globalization;
using System.Windows.Data;
using MRMS.Core.Enums;

namespace MRMS.UI.Converters;

public class UserRoleConverter : IValueConverter
{
    public static string ToArabic(UserRole role) => role switch
    {
        UserRole.Owner => "مالك",
        UserRole.Manager => "مدير",
        UserRole.Receptionist => "استقبال",
        UserRole.Technician => "فني",
        _ => role.ToString()
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is UserRole r ? ToArabic(r) : value?.ToString() ?? "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
