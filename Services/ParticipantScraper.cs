using BilderbergImport.Models;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace BilderbergImport.Services;

public static class ParticipantScraper
{
    public static List<Participant> ParseParticipants(string htmlContent)
    {
        var participants = new List<Participant>(); 
        
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        // Strategy 1: Look for paragraphs that contain participant patterns
        var paragraphNodes = htmlDoc.DocumentNode.SelectNodes("//p");
        if (paragraphNodes != null)
        {
            foreach (var paragraph in paragraphNodes)
            {
                // Skip location/date paragraphs (they have <strong> tags)
                if (paragraph.SelectSingleNode(".//strong") != null)
                {
                    continue;
                }

                // Get the inner HTML of the paragraph
                var innerHtml = paragraph.InnerHtml;

                // Split by <br> tags to get individual lines
                var lines = SplitByBrTags(innerHtml);

                foreach (var line in lines)
                {
                    var participant = ParseParticipantLine(line);
                    if (participant != null)
                    {
                        participants.Add(participant);
                    }
                }
            }
        }

        return participants;
    }

    private static List<string> SplitByBrTags(string html)
    {
        var lines = new List<string>();

        // Split by <br> tags (with or without closing slash)
        var brMatches = Regex.Matches(html, @"(?<line>.*?)(?:<br\s*/?>|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in brMatches)
        {
            if (match.Success)
            {
                var line = match.Groups["line"].Value.Trim();
                if (!string.IsNullOrEmpty(line) && line != "&nbsp;")
                {
                    lines.Add(line);
                }
            }
        }

        return lines;
    }

    private static Participant ParseParticipantLine(string line)
    {
        try
        {
            // Clean the line - remove HTML tags but preserve text
            line = Regex.Replace(line, @"<[^>]*>", " ").Trim();
            line = Regex.Replace(line, @"\s+", " ");

            // Check if this looks like a participant line
            // Participant lines have: LastName, FirstName (CountryCode)
            var participantPattern = @"^(?<lastname>[^,]+),\s*(?<firstname>[^(]+?)\s*\((?<country>[A-Z]{2,3})\)\s*(?:,\s*(?<title>.+))?$";
            var match = Regex.Match(line, participantPattern);

            if (match.Success)
            {
                var lastName = match.Groups["lastname"].Value.Trim();
                var firstName = match.Groups["firstname"].Value.Trim();
                var countryCode = match.Groups["country"].Value;
                var title = match.Groups["title"].Success ?
                    CleanTitle(match.Groups["title"].Value) : null;

                // Handle special cases
                (firstName, lastName) = NormalizeName(firstName, lastName);

                return new Participant
                {
                    FirstName = firstName,
                    LastName = lastName,
                    CountryCode = countryCode,
                    Title = title
                };
            }

            // Try pattern without country code
            participantPattern = @"^(?<lastname>[^,]+),\s*(?<firstname>[^,]+?)(?:,\s*(?<title>.+))?$";
            match = Regex.Match(line, participantPattern);

            if (match.Success)
            {
                var lastName = match.Groups["lastname"].Value.Trim();
                var firstName = match.Groups["firstname"].Value.Trim();
                var title = match.Groups["title"].Success ?
                    CleanTitle(match.Groups["title"].Value) : null;

                (firstName, lastName) = NormalizeName(firstName, lastName);

                return new Participant
                {
                    FirstName = firstName,
                    LastName = lastName,
                    CountryCode = null,
                    Title = title
                };
            }

            // Skip lines that don't look like participants
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing line '{line}': {ex.Message}");
            return null;
        }
    }

    private static (string FirstName, string LastName) NormalizeName(string firstName, string lastName)
    {
        // Handle special cases
        if (lastName.Equals("Netherlands", StringComparison.OrdinalIgnoreCase))
        {
            // "Netherlands, H.M. the King of the (NLD)"
            if (firstName.Contains("the King of the"))
            {
                return ("H.M. the King of the", "Netherlands");
            }
        }

        // Trim and return
        return (firstName.Trim(), lastName.Trim());
    }

    private static string CleanTitle(string title)
    {
        title = title.Trim();

        // Remove leading punctuation
        if (title.StartsWith(',') || title.StartsWith(';') || title.StartsWith(':'))
        {
            title = title.Substring(1).Trim();
        }

        // Replace HTML entities
        title = title.Replace("&amp;", "& ");
        title = title.Replace("&nbsp;", " ");

        return title;
    }
}
