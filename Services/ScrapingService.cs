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

    public async Task ImportMeetingsFromUrl(string url)
    {
        var web = new HtmlWeb();
        var htmlDoc = await Task.Run(() => web.Load(url));

        var meetings = await ScrapeMeetingsAsync(htmlDoc);
        foreach (var meeting in meetings)
        {
            await _dataService.AddMeetingAsync(meeting);
        }
    }

    public async Task ImportParticipantsFromUrl(string url, int meetingId)
    {
        var web = new HtmlWeb();
        var htmlDoc = await Task.Run(() => web.Load(url));

        var participants = await ScrapeParticipantsAsync(htmlDoc);
        foreach (var participant in participants)
        {
            // Add participant to database
            var addedParticipant = await _dataService.AddParticipantAsync(participant);

            // Link participant to selected meeting
            await _dataService.AddMeetingParticipantAsync(meetingId, addedParticipant.Id);
        }
    }

    public async Task ImportTopicsFromUrl(string url, int meetingId)
    {
        var web = new HtmlWeb();
        var htmlDoc = await Task.Run(() => web.Load(url));

        var topics = await ScrapeMeetingTopicsAsync(htmlDoc, meetingId);
        foreach (var topic in topics)
        {
            await _dataService.AddMeetingTopicAsync(topic);
        }
    }

    private async Task<List<Participant>> ScrapeParticipantsAsync(HtmlDocument htmlDoc)
    {
        var participants = new List<Participant>();

        // Customize this based on your actual website structure
        // Example: scraping from a table
        var tableRows = htmlDoc.DocumentNode.SelectNodes("//table[@id='participants-table']//tr");

        if (tableRows != null)
        {
            foreach (var row in tableRows.Skip(1)) // Skip header
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

        return await Task.FromResult(participants);
    }

    private async Task<List<Meeting>> ScrapeMeetingsAsync(HtmlDocument htmlDoc)
    {
        var meetings = new List<Meeting>();

        // Customize this based on your actual website structure
        var meetingSections = htmlDoc.DocumentNode.SelectNodes("//div[@class='meeting']");

        if (meetingSections != null)
        {
            foreach (var section in meetingSections)
            {
                try
                {
                    var meeting = new Meeting
                    {
                        Year = short.Parse(section.SelectSingleNode(".//span[@class='year']")?.InnerText.Trim() ?? "0"),
                        FromDate = DateTime.Parse(section.SelectSingleNode(".//span[@class='from-date']")?.InnerText.Trim() ?? DateTime.Now.ToString()),
                        ToDate = DateTime.Parse(section.SelectSingleNode(".//span[@class='to-date']")?.InnerText.Trim() ?? DateTime.Now.ToString()),
                        Location = section.SelectSingleNode(".//span[@class='location']")?.InnerText.Trim() ?? "Unknown",
                        Description = section.SelectSingleNode(".//div[@class='description']")?.InnerText.Trim()
                    };

                    meetings.Add(meeting);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing meeting: {ex.Message}");
                }
            }
        }

        return await Task.FromResult(meetings);
    }

    private async Task<List<MeetingTopic>> ScrapeMeetingTopicsAsync(HtmlDocument htmlDoc, int meetingId)
    {
        var topics = new List<MeetingTopic>();

        // Customize this based on your actual website structure
        var topicNodes = htmlDoc.DocumentNode.SelectNodes("//ul[@class='topics-list']/li");

        if (topicNodes != null)
        {
            foreach (var topicNode in topicNodes)
            {
                topics.Add(new MeetingTopic
                {
                    MeetingId = meetingId,
                    Topic = topicNode.InnerText.Trim()
                });
            }
        }

        return await Task.FromResult(topics);
    }
}

