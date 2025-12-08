using BilderbergImport.Data;
using BilderbergImport.Models;
using Microsoft.EntityFrameworkCore;

namespace BilderbergImport.Services;

public class DataService
{
    private readonly string _connectionString;

    public DataService(string connectionString)
    {
        _connectionString = connectionString;
    }

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

    public async Task<Participant> GetParticipantByIdAsync(int id)
    {
        using var context = CreateContext();
        return await context.Participants
            .Include(p => p.MeetingParticipants)
            .FirstOrDefaultAsync(p => p.Id == id);
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

    public async Task<Meeting> GetMeetingByIdAsync(int id)
    {
        using var context = CreateContext();
        return await context.Meetings
            .Include(m => m.MeetingParticipants)
            .FirstOrDefaultAsync(m => m.Id == id);
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

    public async Task RemoveMeetingParticipantAsync(int meetingId, int participantId)
    {
        using var context = CreateContext();
        var meetingParticipant = await context.MeetingParticipants
            .FirstOrDefaultAsync(mp => mp.MeetingId == meetingId && mp.ParticipantId == participantId);

        if (meetingParticipant != null)
        {
            context.MeetingParticipants.Remove(meetingParticipant);
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
            .OrderByDescending(mt => mt.Meeting.Year)
            .ThenBy(mt => mt.Topic)
            .ToListAsync();
    }

    public async Task AddMeetingTopicAsync(MeetingTopic topic)
    {
        using var context = CreateContext();
        context.MeetingTopics.Add(topic);
        await context.SaveChangesAsync();
    }

    public async Task ClearDatabaseAsync()
    {
        using var context = CreateContext();
        context.MeetingParticipants.RemoveRange(context.MeetingParticipants);
        context.MeetingTopics.RemoveRange(context.MeetingTopics);
        context.Meetings.RemoveRange(context.Meetings);
        context.Participants.RemoveRange(context.Participants);
        await context.SaveChangesAsync();
    }

}

