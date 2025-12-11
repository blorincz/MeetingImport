using BilderbergImport.Models;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace BilderbergImport.Services;

public class TableParticipantParser
{
    public static List<Participant> ParseParticipantsFromTable(string htmlContent)
    {
        var participants = new List<Participant>();

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        // Find all table rows
        var tableRows = htmlDoc.DocumentNode.SelectNodes("//table//tr");
        if (tableRows == null) return participants;

        foreach (var row in tableRows)
        {
            // Skip header rows if they exist
            if (row.SelectSingleNode(".//th") != null)
            {
                continue;
            }

            var participant = ParseTableRow(row);
            if (participant != null)
            {
                participants.Add(participant);
            }
        }

        return participants;
    }

    private static Participant ParseTableRow(HtmlNode row)
    {
        try
        {
            // Get all table cells in this row
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 3)
            {
                return null;
            }

            // Column 1: Country Code
            var countryCode = cells[0].InnerText.Trim();

            // Column 2: Name (LastName, FirstName)
            var nameText = cells[1].InnerText.Trim();

            // Column 3: Title
            var title = cells[2].InnerText.Trim();

            // Parse the name
            var (firstName, lastName) = ParseName(nameText);

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                Console.WriteLine($"Could not parse name from: {nameText}");
                return null;
            }

            // Clean the title
            title = CleanTitle(title);

            return new Participant
            {
                FirstName = firstName,
                LastName = lastName,
                CountryCode = countryCode,
                Title = string.IsNullOrEmpty(title) ? null : title
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing table row: {ex.Message}");
            return null;
        }
    }

    private static (string FirstName, string LastName) ParseName(string nameText)
    {
        // Expected format: "LastName, FirstName"
        // Could also be: "Netherlands, H.R.H. Princess Beatrix of The"

        nameText = nameText.Trim();

        // Split by first comma
        var commaIndex = nameText.IndexOf(',');
        if (commaIndex == -1)
        {
            // No comma found - try to parse as "FirstName LastName"
            return ParseWithoutComma(nameText);
        }

        var lastName = nameText.Substring(0, commaIndex).Trim();
        var firstName = nameText[(commaIndex + 1)..].Trim();

        // Handle names with middle initials: "Achleitner, Paul M."
        // Handle names with prefixes: "Castries, Henri de"

        return (firstName, lastName);
    }

    private static (string FirstName, string LastName) ParseWithoutComma(string nameText)
    {
        // Try to split by spaces - last word is last name
        var nameParts = nameText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (nameParts.Length == 0)
        {
            return ("", "");
        }
        else if (nameParts.Length == 1)
        {
            return (nameParts[0], "");
        }
        else
        {
            var lastName = nameParts[^1]; // Last part
            var firstName = string.Join(" ", nameParts[..^1]); // All but last
            return (firstName, lastName);
        }
    }

    private static string CleanTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return "";

        // Remove HTML entities
        title = title.Replace("&amp;", "&");
        title = title.Replace("&nbsp;", " ");

        // Remove any HTML tags
        title = Regex.Replace(title, @"<[^>]*>", "");

        // Clean up whitespace
        title = Regex.Replace(title, @"\s+", " ").Trim();

        return title;
    }
}
