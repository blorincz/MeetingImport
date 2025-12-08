using BilderbergImport.Models;
using Microsoft.EntityFrameworkCore;

namespace BilderbergImport.Data;

public class BilderbergDbContext : DbContext
{
    private readonly string _connectionString;

    public DbSet<Participant> Participants { get; set; }
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<MeetingTopic> MeetingTopics { get; set; }
    public DbSet<MeetingParticipant> MeetingParticipants { get; set; }

    // Constructor for dependency injection
    public BilderbergDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public BilderbergDbContext(DbContextOptions<BilderbergDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MeetingParticipant>()
                .HasOne(mp => mp.Meeting)
                .WithMany(m => m.MeetingParticipants)
                .HasForeignKey(mp => mp.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MeetingParticipant>()
            .HasOne(mp => mp.Participant)
            .WithMany(p => p.MeetingParticipants)
            .HasForeignKey(mp => mp.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MeetingTopic>()
            .HasOne(mt => mt.Meeting)
            .WithMany(m => m.MeetingTopics)
            .HasForeignKey(mt => mt.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

