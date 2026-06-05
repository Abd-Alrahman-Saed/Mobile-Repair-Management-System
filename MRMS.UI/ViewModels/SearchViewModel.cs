using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;
using MRMS.UI.Services;

namespace MRMS.UI.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly AppNavigationService _navigationService;

    [ObservableProperty]
    private string _query = "";

    [ObservableProperty]
    private ObservableCollection<SearchResultDto> _results = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _hasSearched;

    public SearchViewModel(ISearchService searchService, AppNavigationService navigationService)
    {
        _searchService = searchService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query)) return;

        IsLoading = true;
        HasSearched = true;
        var items = await _searchService.SearchAsync(Query);
        Results = new ObservableCollection<SearchResultDto>(items);
        HasResults = Results.Count > 0;
        IsLoading = false;
    }

    [RelayCommand]
    private void OpenResult(SearchResultDto? result)
    {
        if (result is null) return;

        if (result.Type == "جهاز")
        {
            var customerId = result.CustomerId ?? result.Id;
            var page = App.ServiceProvider!.GetRequiredService<Views.CustomerProfilePage>();
            _navigationService.NavigateTo(page, "CustomerId", customerId);
        }
        else if (result.Type == "تذكرة")
        {
            var page = App.ServiceProvider!.GetRequiredService<Views.RepairTicketDetailPage>();
            _navigationService.NavigateTo(page, "TicketId", result.Id);
        }
    }
}
