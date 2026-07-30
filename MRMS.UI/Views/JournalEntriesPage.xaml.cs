using System.Windows.Controls;
using MRMS.Application.DTOs;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class JournalEntriesPage : Page
{
    private readonly JournalEntriesViewModel _vm;

    public JournalEntriesPage(JournalEntriesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) => await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void EntriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntriesGrid.SelectedItem is JournalEntryDto entry)
            LinesGrid.ItemsSource = entry.Lines;
    }
}
