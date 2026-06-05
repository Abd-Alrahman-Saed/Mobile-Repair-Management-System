using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace MRMS.UI.Services;

public class NavigationEntry
{
    public Type PageType { get; set; } = null!;
    public Dictionary<string, object?> Parameters { get; set; } = [];
}

public class AppNavigationService
{
    private Frame? _frame;
    private readonly Dictionary<string, object?> _params = [];
    private readonly Stack<NavigationEntry> _history = new();
    private readonly IServiceProvider _serviceProvider;

    public AppNavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize(Frame frame) => _frame = frame;

    public bool CanGoBack => _history.Count > 0;

    public void NavigateTo(Page page)
    {
        SaveCurrentToHistory();
        _params.Clear();
        _frame?.Navigate(page);
    }

    public void NavigateTo(Page page, string key, object? value)
    {
        SaveCurrentToHistory();
        _params.Clear();
        _params[key] = value;
        _frame?.Navigate(page);
    }

    private void SaveCurrentToHistory()
    {
        if (_frame?.Content is Page currentPage)
        {
            _history.Push(new NavigationEntry
            {
                PageType = currentPage.GetType(),
                Parameters = new Dictionary<string, object?>(_params)
            });
        }
    }

    public T? GetParameter<T>(string key)
    {
        if (_params.TryGetValue(key, out var val) && val is T tVal)
            return tVal;
        return default;
    }

    public void GoBack()
    {
        if (_history.TryPop(out var entry))
        {
            var page = (Page)_serviceProvider.GetRequiredService(entry.PageType);
            _params.Clear();
            foreach (var kvp in entry.Parameters)
                _params[kvp.Key] = kvp.Value;
            _frame?.Navigate(page);
        }
    }
}
