using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BilderbergImport.Models;
using BilderbergImport.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace BilderbergImport.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DataService _dataService;
    private readonly ScrapingService _scrapingService;
    private readonly ConfigurationService _configurationService;

    [ObservableProperty]
    private string _url;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isImporting;

    // Import URLs
    [ObservableProperty]
    private string _participantsUrl = string.Empty;

    [ObservableProperty]
    private string _meetingsUrl = string.Empty;

    [ObservableProperty]
    private string _topicsUrl = string.Empty;

    // Selected meeting for participants import
    [ObservableProperty]
    private Meeting _selectedMeeting;

    [ObservableProperty]
    private ObservableCollection<Participant> _participants = [];

    [ObservableProperty]
    private ObservableCollection<Meeting> _meetings = [];

    [ObservableProperty]
    private ObservableCollection<MeetingTopic> _meetingTopics = [];

    [ObservableProperty]
    private ObservableCollection<MeetingParticipant> _meetingParticipants = [];

    [ObservableProperty]
    private ObservableCollection<object> _selectedTabItems = [];

    public MainViewModel()
    {
        // Initialize configuration
        _configurationService = new ConfigurationService();

        // Get connection string from configuration
        var connectionString = _configurationService.GetConnectionString();

        _dataService = new DataService(connectionString);
        _scrapingService = new ScrapingService(_dataService);

        // Get default URL from configuration
        Url = _configurationService.GetSetting("AppSettings:DefaultUrl")
            ?? "https://www.bilderbergmeetings.org/meetings/meeting-2025/press-release-2025";
    }

    [RelayCommand]
    private async Task ImportMeetings()
    {
        if (string.IsNullOrWhiteSpace(MeetingsUrl))
        {
            MessageBox.Show("Please enter a URL for meetings", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsImporting = true;
            Status = "Importing meetings...";

            await _scrapingService.ImportMeetingsFromUrl(MeetingsUrl);

            // Reload meetings
            var meetings = await _dataService.GetMeetingsAsync();
            Meetings = new ObservableCollection<Meeting>(meetings);

            Status = "Meetings imported successfully!";
            MessageBox.Show("Meetings imported successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // Load all data
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

    [RelayCommand]
    private async Task ImportParticipants()
    {
        if (string.IsNullOrWhiteSpace(ParticipantsUrl))
        {
            MessageBox.Show("Please enter a URL for participants", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedMeeting == null)
        {
            MessageBox.Show("Please select a meeting for the participants", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsImporting = true;
            Status = "Importing participants...";

            await _scrapingService.ImportParticipantsFromUrl(ParticipantsUrl, SelectedMeeting.Id);

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

    [RelayCommand]
    private async Task ImportTopics()
    {
        if (string.IsNullOrWhiteSpace(TopicsUrl))
        {
            MessageBox.Show("Please enter a URL for topics", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedMeeting == null)
        {
            MessageBox.Show("Please select a meeting for the topics", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsImporting = true;
            Status = "Importing topics...";

            await _scrapingService.ImportTopicsFromUrl(TopicsUrl, SelectedMeeting.Id);

            Status = "Topics imported successfully!";
            MessageBox.Show("Topics imported successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // Load all data
            await LoadData();
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            MessageBox.Show($"Error importing topics: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            Status = "Loading data...";

            var participants = await _dataService.GetParticipantsAsync();
            var topics = await _dataService.GetMeetingTopicsAsync();
            var meetingParticipants = await _dataService.GetMeetingParticipantsAsync();

            Participants = new ObservableCollection<Participant>(participants);
            MeetingTopics = new ObservableCollection<MeetingTopic>(topics);
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

