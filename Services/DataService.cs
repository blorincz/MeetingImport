using BilderbergImport.Data;
using BilderbergImport.Models;
using Microsoft.EntityFrameworkCore;

namespace BilderbergImport.Services;

public class DataService(string connectionString)
{
    private readonly string _connectionString = connectionString;

    private BilderbergDbContext CreateContext()
    {
        return new BilderbergDbContext(_connectionString);
    }

    public async Task<List<Participant>> GetParticipantsAsync()
    {
        using var context = CreateContext();
        return await context.Participants
            .Include(p => p.MeetingParticipants)
            .ThenInclude(mp => mp.Meeting)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    public async Task<List<Meeting>> GetMeetingsAsync()
    {
        using var context = CreateContext();
        return await context.Meetings
            .Include(m => m.MeetingTopics)
            .Include(m => m.MeetingParticipants)
            .ThenInclude(mp => mp.Participant)
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.FromDate)
            .ToListAsync();
    }

    public async Task<Participant> AddParticipantAsync(Participant participant)
    {
        using var context = CreateContext();
        context.Participants.Add(participant);
        await context.SaveChangesAsync();
        return participant;
    }

    public async Task<Meeting> AddMeetingAsync(Meeting meeting)
    {
        using var context = CreateContext();
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();
        return meeting;
    }

    public async Task AddMeetingParticipantAsync(int meetingId, int participantId)
    {
        using var context = CreateContext();

        // Check if relationship already exists
        var existing = await context.MeetingParticipants
            .FirstOrDefaultAsync(mp => mp.MeetingId == meetingId && mp.ParticipantId == participantId);

        if (existing == null)
        {
            var meetingParticipant = new MeetingParticipant
            {
                MeetingId = meetingId,
                ParticipantId = participantId
            };

            context.MeetingParticipants.Add(meetingParticipant);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<MeetingParticipant>> GetMeetingParticipantsAsync()
    {
        using var context = CreateContext();
        return await context.MeetingParticipants
            .Include(mp => mp.Meeting)
            .Include(mp => mp.Participant)
            .OrderBy(mp => mp.Meeting.Year)
            .ThenBy(mp => mp.Participant.LastName)
            .ToListAsync();
    }

    public async Task<List<MeetingTopic>> GetMeetingTopicsAsync()
    {
        using var context = CreateContext();
        return await context.MeetingTopics
            .Include(mt => mt.Meeting)
            .Include(mt => mt.SubTopics)
            .OrderByDescending(mt => mt.Meeting.Year)
            .ThenBy(mt => mt.Topic)
            .ToListAsync();
    }

    public async Task<List<MeetingTopicSubTopic>> GetMeetingTopicSubTopicsAsync()
    {
        using var context = CreateContext();
        return await context.MeetingTopicSubTopics
            .Include(mts => mts.MeetingTopic)
            .ThenInclude(mt => mt.Meeting)
            .OrderBy(mts => mts.MeetingTopic.Meeting.Year)
            .ThenBy(mts => mts.MeetingTopic.Topic)
            .ThenBy(mts => mts.Topic)
            .ToListAsync();
    }

    public async Task ClearDatabaseAsync()
    {
        using var context = CreateContext();
        context.MeetingTopicSubTopics.RemoveRange(context.MeetingTopicSubTopics);
        context.MeetingParticipants.RemoveRange(context.MeetingParticipants);
        context.MeetingTopics.RemoveRange(context.MeetingTopics);
        context.Meetings.RemoveRange(context.Meetings);
        context.Participants.RemoveRange(context.Participants);
        await context.SaveChangesAsync();
    }

    public async Task<bool> MeetingExistsAsync(short year, DateTime fromDate, string location)
    {
        using var context = CreateContext();
        return await context.Meetings
            .AnyAsync(m => m.Year == year &&
                          m.FromDate == fromDate &&
                          m.Location.ToLower() == location.ToLower());
    }

    public async Task<Participant> GetParticipantByNameAsync(string firstName, string lastName)
    {
        using var context = CreateContext();
        return await context.Participants
            .FirstOrDefaultAsync(p => p.FirstName.ToLower() == firstName.ToLower() &&
                                      p.LastName.ToLower() == lastName.ToLower());
    }

    public async Task<bool> MeetingParticipantExistsAsync(int meetingId, int participantId)
    {
        using var context = CreateContext();
        return await context.MeetingParticipants
            .AnyAsync(mp => mp.MeetingId == meetingId && mp.ParticipantId == participantId);
    }
}

