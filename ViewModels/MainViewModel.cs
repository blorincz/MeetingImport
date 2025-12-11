using BilderbergImport.Models;
using BilderbergImport.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HtmlAgilityPack;
using System.Collections.ObjectModel;
using System.Windows;

namespace BilderbergImport.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DataService _dataService;
    private readonly ScrapingService _scrapingService;

    // HTML Inputs
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportMeetingsCommand))]
    private string _meetingsHtml = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportParticipantsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportParticipantsFromTableCommand))]
    private string _participantsHtml = string.Empty;

    // Selected meeting for participants import
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportParticipantsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ImportParticipantsFromTableCommand))]
    private Meeting? _selectedMeeting;

    [ObservableProperty]
    private ObservableCollection<Meeting> _meetings = new();

    // Status
    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isImporting;

    // Data collections
    [ObservableProperty]
    private ObservableCollection<Participant> _participants = [];

    [ObservableProperty]
    private ObservableCollection<MeetingTopic> _meetingTopics = [];

    [ObservableProperty]
    private ObservableCollection<MeetingTopicSubTopic> _meetingTopicSubTopics = [];

    [ObservableProperty]
    private ObservableCollection<MeetingParticipant> _meetingParticipants = [];

    public MainViewModel()
    {
        var configurationService = new ConfigurationService();
        var connectionString = configurationService.GetConnectionString();
        _dataService = new DataService(connectionString);
        _scrapingService = new ScrapingService(_dataService);

        LoadMeetingsAsync();
    }

    private async void LoadMeetingsAsync()
    {
        try
        {
            var meetings = await _dataService.GetMeetingsAsync();
            Meetings = new ObservableCollection<Meeting>(meetings);

            if (Meetings.Any() && SelectedMeeting == null)
            {
                SelectedMeeting = Meetings.First();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading meetings: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanImportMeetings))]
    private async Task ImportMeetings()
    {
        try
        {
            IsImporting = true;
            Status = "Importing meetings and topics...";

            // Track import statistics
            int importedCount = 0;
            int duplicateCount = 0;
            int errorCount = 0;

            await _scrapingService.ImportMeetingsFromHtml(MeetingsHtml);

            // Reload meetings
            var meetings = await _dataService.GetMeetingsAsync();
            Meetings = new ObservableCollection<Meeting>(meetings);

            MeetingsHtml = string.Empty;

            Status = $"Import complete. Imported: {importedCount}, Duplicates: {duplicateCount}, Errors: {errorCount}";

            if (duplicateCount > 0 || errorCount > 0)
            {
                MessageBox.Show($"Import complete.\nImported: {importedCount}\nDuplicates skipped: {duplicateCount}\nErrors: {errorCount}",
                    "Import Results", MessageBoxButton.OK,
                    duplicateCount > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("Meetings imported successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            await LoadData();
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            MessageBox.Show($"Error importing meetings: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsImporting = false;
        }
    }

    private bool CanImportMeetings() => !string.IsNullOrWhiteSpace(MeetingsHtml) && !IsImporting;

    [RelayCommand(CanExecute = nameof(CanImportParticipants))]
    private async Task ImportParticipants()
    {
        await ImportParticipantsInternal("text");
    }

    [RelayCommand(CanExecute = nameof(CanImportParticipants))]
    private async Task ImportParticipantsFromTable()
    {
        await ImportParticipantsInternal("table");
    }

    [RelayCommand(CanExecute = nameof(CanImportParticipants))]
    private async Task ImportParticipantsInternal(string format)
    {
        try
        {
            IsImporting = true;
            Status = $"Importing participants from {format} format...";

            if (format == "table")
            {
                await _scrapingService.ImportParticipantsFromTable(ParticipantsHtml, SelectedMeeting!.Id);
            }
            else
            {
                await _scrapingService.ImportParticipantsFromHtml(ParticipantsHtml, SelectedMeeting!.Id);
            }

            // Clear the HTML after import
            ParticipantsHtml = string.Empty;

            Status = "Participants imported successfully!";
            MessageBox.Show("Participants imported successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // Load all data
            await LoadData();
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            MessageBox.Show($"Error importing participants: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsImporting = false;
        }
    }

    private bool CanImportParticipants() =>
        !string.IsNullOrWhiteSpace(ParticipantsHtml) &&
        SelectedMeeting != null &&
        !IsImporting;

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            Status = "Loading data...";

            var participants = await _dataService.GetParticipantsAsync();
            var meetings = await _dataService.GetMeetingsAsync(); // Add this
            var topics = await _dataService.GetMeetingTopicsAsync();
            var subTopics = await _dataService.GetMeetingTopicSubTopicsAsync();
            var meetingParticipants = await _dataService.GetMeetingParticipantsAsync();

            Participants = new ObservableCollection<Participant>(participants);
            Meetings = new ObservableCollection<Meeting>(meetings); // Add this
            MeetingTopics = new ObservableCollection<MeetingTopic>(topics);
            MeetingTopicSubTopics = new ObservableCollection<MeetingTopicSubTopic>(subTopics);
            MeetingParticipants = new ObservableCollection<MeetingParticipant>(meetingParticipants);

            Status = "Data loaded successfully!";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ClearDatabase()
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all data? This action cannot be undone.",
            "Confirm Clear",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                Status = "Clearing database...";
                await _dataService.ClearDatabaseAsync();

                // Clear collections
                Participants.Clear();
                Meetings.Clear();
                MeetingTopicSubTopics.Clear();
                MeetingTopics.Clear();
                MeetingParticipants.Clear();

                Status = "Database cleared successfully!";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
                MessageBox.Show($"Error clearing database: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}