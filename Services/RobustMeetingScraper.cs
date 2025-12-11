using HtmlAgilityPack;
using System.Globalization;

namespace BilderbergImport.Services;

public class MeetingTopicData
{
    public string Topic { get; set; }
    public List<string> SubTopics { get; set; }
}

public class MeetingData
{
    public string Description { get; set; } = string.Empty;
    public short Year { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public List<MeetingTopicData> MainTopics { get; set; } = [];
}

public class RobustMeetingScraper
{
    public static List<MeetingData> ExtractMeetings(HtmlDocument htmlDoc)
    {
        var meetings = new List<MeetingData>();

        // Try multiple strategies to find meetings

        // Strategy 1: Look for h2 tags with dates
        var h2Nodes = htmlDoc.DocumentNode.SelectNodes("//h2");
        if (h2Nodes != null)
        {
            foreach (var h2Node in h2Nodes)
            {
                var meeting = ParseH2Meeting(h2Node);
                if (meeting != null)
                {
                    meetings.Add(meeting);
                }
            }
        }

        // Strategy 2: Look for date patterns in the entire document
        if (meetings.Count == 0)
        {
            meetings = ExtractByDatePatterns(htmlDoc);
        }

        return meetings;
    }

    private static MeetingData ParseH2Meeting(HtmlNode h2Node)
    {
        var meetingText = h2Node.InnerText.Trim();

        // Check if this looks like a meeting header
        if (IsMeetingHeader(meetingText))
        {
            var meeting = ParseMeetingHeaderText(meetingText);

            // Find topics (next ul element)
            var ulNode = h2Node.SelectSingleNode("following-sibling::ul[1]");
            if (ulNode != null)
            {
                ExtractTopicsWithSubtopics(ulNode, ref meeting);
            }

            return meeting;
        }

        return null;
    }

    private static void ExtractTopicsWithSubtopics(HtmlNode ulNode, ref MeetingData meeting)
    {
        var liNodes = ulNode.SelectNodes(".//li");

        if (liNodes == null) return;

        MeetingTopicData currentTopic = null;

        foreach (var liNode in liNodes)
        {
            var text = liNode.InnerText.Trim();

            // Check if text is empty after trimming
            if (string.IsNullOrEmpty(text)) continue;

            var isSubTopic = text[0] == '-' || text[0] == '–' || text[0] == '•';

            if (isSubTopic)
            {
                // Remove the bullet character and trim again
                var subTopicText = text[1..].Trim();

                if (!string.IsNullOrEmpty(subTopicText))
                {
                    // Create a topic if we don't have one for orphaned subtopics
                    currentTopic ??= CreateTopicForOrphanedSubtopic();

                    currentTopic.SubTopics.Add(subTopicText);
                }
            }
            else
            {
                // Save previous topic and start new one
                if (currentTopic != null)
                {
                    meeting.MainTopics.Add(currentTopic);
                }

                currentTopic = new MeetingTopicData
                {
                    Topic = text,
                    SubTopics = []
                };
            }
        }

        // Add the last topic
        if (currentTopic != null && !meeting.MainTopics.Contains(currentTopic))
        {
            meeting.MainTopics.Add(currentTopic);
        }
    }

    private static MeetingTopicData CreateTopicForOrphanedSubtopic()
    {
        var topic = new MeetingTopicData
        {
            Topic = "", // Empty main topic text
            SubTopics = []
        };

        return topic;
    }

    private static bool IsMeetingHeader(string text)
    {
        // Check for date patterns
        return System.Text.RegularExpressions.Regex.IsMatch(text,
            @"\d{1,2}[-–]\d{1,2}\s+[A-Za-z]+\s+\d{4}") ||
               System.Text.RegularExpressions.Regex.IsMatch(text,
            @"\d{1,2}\s+[A-Za-z]+\s+\d{4}");
    }

    private static MeetingData ParseMeetingHeaderText(string headerText)
    {
        var meeting = new MeetingData { Description = headerText };

        // Try to extract using regex
        var match = System.Text.RegularExpressions.Regex.Match(headerText,
            @"(?<start>\d{1,2})(?:[-–](?<end>\d{1,2}))?\s+(?<month>[A-Za-z]+)\s+(?<year>\d{4})\s+(?<location>.+)$");

        if (match.Success)
        {
            meeting.Year = short.Parse(match.Groups["year"].Value);

            var monthName = match.Groups["month"].Value;
            var startDay = int.Parse(match.Groups["start"].Value);
            var endDay = match.Groups["end"].Success ?
                int.Parse(match.Groups["end"].Value) : startDay;
            meeting.Location = match.Groups["location"].Value.Trim();

            // Parse dates
            if (DateTime.TryParseExact($"{startDay} {monthName} {meeting.Year}",
                "d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
            {
                meeting.FromDate = fromDate;
            }

            if (DateTime.TryParseExact($"{endDay} {monthName} {meeting.Year}",
                "d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
            {
                meeting.ToDate = toDate;
            }
        }

        return meeting;
    }

    private static List<string> ExtractListItems(HtmlNode ulNode)
    {
        var items = new List<string>();
        var liNodes = ulNode.SelectNodes(".//li");

        if (liNodes != null)
        {
            foreach (var liNode in liNodes)
            {
                items.Add(liNode.InnerText.Trim());
            }
        }

        return items;
    }

    private static List<MeetingData> ExtractByDatePatterns(HtmlDocument htmlDoc)
    {
        var meetings = new List<MeetingData>();
        var text = htmlDoc.DocumentNode.InnerText;

        // Find all date patterns
        var matches = System.Text.RegularExpressions.Regex.Matches(text,
            @"(\d{1,2}[-–]\d{1,2}\s+[A-Za-z]+\s+\d{4}\s+[^\n\r]+)");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Success)
            {
                var meeting = ParseMeetingHeaderText(match.Value);
                meetings.Add(meeting);
            }
        }

        return meetings;
    }
}