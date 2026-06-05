using System.Windows.Controls;
using MRMS.UI.ViewModels;

namespace MRMS.UI.Views;

public partial class SearchPage : Page
{
    public SearchPage(SearchViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
