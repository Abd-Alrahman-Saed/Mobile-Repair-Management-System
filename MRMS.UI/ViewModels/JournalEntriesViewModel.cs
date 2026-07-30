using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MRMS.Application.DTOs;
using MRMS.Application.Interfaces;

namespace MRMS.UI.ViewModels;

public partial class JournalEntriesViewModel : ObservableObject
{
    private readonly IJournalService _journalService;

    public JournalEntriesViewModel(IJournalService journalService)
    {
        _journalService = journalService;
    }

    [ObservableProperty]
    private ObservableCollection<JournalEntryDto> _entries = [];

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        var entries = await _journalService.GetAllEntriesAsync();
        Entries = new ObservableCollection<JournalEntryDto>(entries);
        IsLoading = false;
    }
}
