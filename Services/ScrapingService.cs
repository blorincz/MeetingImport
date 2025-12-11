using HtmlAgilityPack;
using BilderbergImport.Models;

namespace BilderbergImport.Services;

public class ScrapingService
{
    private readonly DataService _dataService;

    public ScrapingService(DataService dataService)
    {
        _dataService = dataService;
    }

    private class TopicData
    {
        public string MainTopic { get; set; } = string.Empty;
        public List<string> SubTopics { get; set; } = new();
    }

    public async Task ImportMeetingsFromHtml(string htmlContent)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        //var meetingDataList = RobustMeetingScraper.ExtractMeetings(htmlDoc);
        var meetings = await ScrapeMeetingsAsync(htmlDoc);

        foreach (var meeting in meetings)
        {
            try
            {
                // Check if meeting already exists
                bool meetingExists = await _dataService.MeetingExistsAsync(
                    meeting.Year,
                    meeting.FromDate,
                    meeting.Location);

                if (!meetingExists)
                {
                    var addedMeeting = await _dataService.AddMeetingAsync(meeting);
                }
                else
                {
                    // Meeting already exists - you could update it or skip
                    Console.WriteLine($"Meeting already exists: {meeting.Year} - {meeting.Location}");
                }
            }
            catch (InvalidOperationException ex)
            {
                // Log duplicate error and continue
                Console.WriteLine($"Duplicate meeting skipped: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing meeting: {ex.Message}");
            }
        }
    }

    public async Task ImportParticipantsFromHtml(string htmlContent, int meetingId)
    {
        var participants = ParticipantScraper.ParseParticipants(htmlContent);
        await ProcessParticipants(participants, meetingId);
    }

    public async Task ImportParticipantsFromTable(string htmlContent, int meetingId)
    {
        var participants = TableParticipantParser.ParseParticipantsFromTable(htmlContent);
        await ProcessParticipants(participants, meetingId);
    }

    private async Task ProcessParticipants(List<Participant> participants, int meetingId)
    {
        foreach (var participant in participants)
        {
            try
            {
                // Check if participant already exists
                var existingParticipant = await _dataService.GetParticipantByNameAsync(
                    participant.FirstName,
                    participant.LastName);

                Participant addedParticipant;

                if (existingParticipant == null)
                {
                    // Add new participant
                    addedParticipant = await _dataService.AddParticipantAsync(participant);
                }
                else
                {
                    // Use existing participant
                    addedParticipant = existingParticipant;
                    Console.WriteLine($"Participant already exists: {participant.FirstName} {participant.LastName}");
                }

                // Check if relationship already exists
                var relationshipExists = await _dataService.MeetingParticipantExistsAsync(
                    meetingId,
                    addedParticipant.Id);

                if (!relationshipExists)
                {
                    // Link participant to meeting
                    await _dataService.AddMeetingParticipantAsync(meetingId, addedParticipant.Id);
                }
                else
                {
                    Console.WriteLine($"Participant already linked to meeting: {participant.FirstName} {participant.LastName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing participant: {ex.Message}");
            }
        }
    }

    public async Task ImportTopicsForMeeting(MeetingData meetingData, int meetingId)
    {
        foreach (var topicData in meetingData.MainTopics)
        {
            // Check if topic already exists for this meeting
            var existingTopic = await _dataService.GetTopicByMeetingAndTextAsync(meetingId, topicData.Topic);

            if (existingTopic == null)
            {
                var topic = new MeetingTopic
                {
                    MeetingId = meetingId,
                    Topic = topicData.Topic
                };

                int addedTopicId = await _dataService.AddMeetingTopicAsync(topic);

                // Import subtopics
                foreach (var subTopicText in topicData.SubTopics)
                {
                    var subTopic = new MeetingTopicSubTopic
                    {
                        TopicId = addedTopicId,
                        Topic = subTopicText
                    };

                    await _dataService.AddMeetingTopicSubTopicAsync(subTopic);
                }
            }
        }
    }

    private async Task<List<Participant>> ScrapeParticipantsAsync(HtmlDocument htmlDoc)
    {
        var participants = new List<Participant>();

        // Customize this based on your actual HTML structure
        var tableRows = htmlDoc.DocumentNode.SelectNodes("//table//tr");

        if (tableRows != null)
        {
            foreach (var row in tableRows.Skip(1)) // Skip header if present
            {
                var cells = row.SelectNodes("td");
                if (cells != null && cells.Count >= 4)
                {
                    participants.Add(new Participant
                    {
                        FirstName = cells[0].InnerText.Trim(),
                        LastName = cells[1].InnerText.Trim(),
                        Title = cells[2].InnerText.Trim(),
                        CountryCode = cells[3].InnerText.Trim()
                    });
                }
            }
        }

        // Alternative: Look for list items
        if (participants.Count == 0)
        {
            var listItems = htmlDoc.DocumentNode.SelectNodes("//li");
            if (listItems != null)
            {
                foreach (var item in listItems)
                {
                    var text = item.InnerText.Trim();
                    // Parse participant from text (customize based on your format)
                    var participant = ParseParticipantFromText(text);
                    if (participant != null)
                    {
                        participants.Add(participant);
                    }
                }
            }
        }

        return await Task.FromResult(participants);
    }

    private Participant ParseParticipantFromText(string text)
    {
        // Customize this based on your participant format
        // Example: "John Doe (USA) - CEO"
        var match = System.Text.RegularExpressions.Regex.Match(text,
            @"^(?<first>\w+)\s+(?<last>\w+)\s*(?:\((?<country>\w+)\))?\s*(?:-\s*(?<title>.+))?$");

        if (match.Success)
        {
            return new Participant
            {
                FirstName = match.Groups["first"].Value,
                LastName = match.Groups["last"].Value,
                CountryCode = match.Groups["country"].Success ? match.Groups["country"].Value : null,
                Title = match.Groups["title"].Success ? match.Groups["title"].Value : null
            };
        }

        return null;
    }

    private static async Task<List<Meeting>> ScrapeMeetingsAsync(HtmlDocument htmlDoc)
    {
        var meetings = new List<Meeting>();

        // Use the meeting scraper from previous response
        var meetingDataList = RobustMeetingScraper.ExtractMeetings(htmlDoc);

        foreach (var meetingData in meetingDataList)
        {
            try
            {
                var meeting = new Meeting
                {
                    Year = meetingData.Year,
                    FromDate = meetingData.FromDate,
                    ToDate = meetingData.ToDate,
                    Location = meetingData.Location,
                    Description = meetingData.Description
                };

                foreach (MeetingTopicData topic in meetingData.MainTopics)
                {
                    MeetingTopic newMeetingTopic = new() { Meeting = meeting, Topic = topic.Topic };

                    foreach (string subtopic in topic.SubTopics)
                    {
                        newMeetingTopic.SubTopics.Add(
                            new MeetingTopicSubTopic { MeetingTopic = newMeetingTopic, Topic = subtopic });
                    }

                    meeting.MeetingTopics.Add(newMeetingTopic);
                }

                meetings.Add(meeting);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating meeting from scraped data: {ex.Message}");
            }
        }

        return await Task.FromResult(meetings);
    }

    private static async Task<List<TopicData>> ScrapeMeetingTopicsAsync(HtmlDocument htmlDoc, int meetingId)
    {
        var topicsData = new List<TopicData>();

        // Customize this based on your actual HTML structure
        var ulNodes = htmlDoc.DocumentNode.SelectNodes("//ul");

        if (ulNodes != null)
        {
            foreach (var ulNode in ulNodes)
            {
                var liNodes = ulNode.SelectNodes(".//li");
                if (liNodes != null)
                {
                    var currentTopic = new TopicData();

                    foreach (var liNode in liNodes)
                    {
                        var text = liNode.InnerText.Trim();

                        // Check if this is a subtopic (starts with dash or hyphen)
                        if (text.StartsWith('-') || text.StartsWith('–') || text.StartsWith('•'))
                        {
                            // Remove the dash/hyphen/bullet and trim
                            var subTopicText = text.Substring(1).Trim();
                            if (!string.IsNullOrEmpty(subTopicText))
                            {
                                currentTopic.SubTopics.Add(subTopicText);
                            }
                        }
                        else
                        {
                            // This is a new main topic
                            if (!string.IsNullOrEmpty(currentTopic.MainTopic))
                            {
                                topicsData.Add(currentTopic);
                            }

                            currentTopic = new TopicData
                            {
                                MainTopic = text
                            };
                        }
                    }

                    // Add the last topic
                    if (!string.IsNullOrEmpty(currentTopic.MainTopic))
                    {
                        topicsData.Add(currentTopic);
                    }
                }
            }
        }

        return await Task.FromResult(topicsData);
    }
}

