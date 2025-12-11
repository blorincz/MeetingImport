using HtmlAgilityPack;
using BilderbergImport.Models;

namespace BilderbergImport.Services;

public class ScrapingService(DataService dataService)
{
    private readonly DataService _dataService = dataService;

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

    private static async Task<List<Meeting>> ScrapeMeetingsAsync(HtmlDocument htmlDoc)
    {
        var meetings = new List<Meeting>();

        // Use the meeting scraper from previous response
        var meetingDataList = MeetingScraper.ExtractMeetings(htmlDoc);

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

}

