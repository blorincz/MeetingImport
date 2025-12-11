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

    // Alternative: AddOrGetMeetingAsync (returns existing if found)
    public async Task<Meeting> AddOrGetMeetingAsync(Meeting meeting)
    {
        using var context = CreateContext();

        var existing = await context.Meetings
            .FirstOrDefaultAsync(m => m.Year == meeting.Year &&
                                     m.FromDate == meeting.FromDate &&
                                     m.Location.ToLower() == meeting.Location.ToLower());

        if (existing != null)
        {
            return existing; // Return existing meeting
        }

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
            .Include(mt => mt.SubTopics)
            .OrderByDescending(mt => mt.Meeting.Year)
            .ThenBy(mt => mt.Topic)
            .ToListAsync();
    }

    public async Task<int> AddMeetingTopicAsync(MeetingTopic topic)
    {
        using var context = CreateContext();
        context.MeetingTopics.Add(topic);
        return await context.SaveChangesAsync();
    }

    public async Task AddMeetingTopicSubTopicAsync(MeetingTopicSubTopic subTopic)
    {
        using var context = CreateContext();
        context.MeetingTopicSubTopics.Add(subTopic);
        await context.SaveChangesAsync();
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

    public async Task<List<MeetingTopicSubTopic>> GetSubTopicsByTopicIdAsync(int topicId)
    {
        using var context = CreateContext();
        return await context.MeetingTopicSubTopics
            .Where(mts => mts.TopicId == topicId)
            .OrderBy(mts => mts.Topic)
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

    // Check if a meeting already exists
    public async Task<bool> MeetingExistsAsync(short year, DateTime fromDate, string location)
    {
        using var context = CreateContext();
        return await context.Meetings
            .AnyAsync(m => m.Year == year &&
                          m.FromDate == fromDate &&
                          m.Location.ToLower() == location.ToLower());
    }

    // Check if a participant already exists
    public async Task<bool> ParticipantExistsAsync(string firstName, string lastName)
    {
        using var context = CreateContext();
        return await context.Participants
            .AnyAsync(p => p.FirstName.ToLower() == firstName.ToLower() &&
                           p.LastName.ToLower() == lastName.ToLower());
    }

    // Get existing participant or null
    public async Task<Participant> GetParticipantByNameAsync(string firstName, string lastName)
    {
        using var context = CreateContext();
        return await context.Participants
            .FirstOrDefaultAsync(p => p.FirstName.ToLower() == firstName.ToLower() &&
                                      p.LastName.ToLower() == lastName.ToLower());
    }

    // Check if meeting-participant relationship already exists
    public async Task<bool> MeetingParticipantExistsAsync(int meetingId, int participantId)
    {
        using var context = CreateContext();
        return await context.MeetingParticipants
            .AnyAsync(mp => mp.MeetingId == meetingId && mp.ParticipantId == participantId);
    }

    // Get meeting by unique properties
    public async Task<Meeting> GetMeetingByPropertiesAsync(short year, DateTime fromDate, string location)
    {
        using var context = CreateContext();
        return await context.Meetings
            .FirstOrDefaultAsync(m => m.Year == year &&
                                     m.FromDate == fromDate &&
                                     m.Location.ToLower() == location.ToLower());
    }

    public async Task<MeetingTopic> GetTopicByMeetingAndTextAsync(int meetingId, string topicText)
    {
        using var context = CreateContext();
        return await context.MeetingTopics
            .FirstOrDefaultAsync(mt => mt.MeetingId == meetingId &&
                                      mt.Topic.ToLower() == topicText.ToLower());
    }

}

