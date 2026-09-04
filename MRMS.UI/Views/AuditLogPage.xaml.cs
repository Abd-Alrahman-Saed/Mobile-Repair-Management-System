using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class AuditLogPage : Page
{
    private readonly AuditLogViewModel _vm;

    public AuditLogPage(AuditLogViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);
    }
}
