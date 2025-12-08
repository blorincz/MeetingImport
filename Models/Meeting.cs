using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilderbergImport.Models;

public class Meeting
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public short Year { get; set; }

    [Required]
    public DateTime FromDate { get; set; }

    [Required]
    public DateTime ToDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; }

    public ICollection<MeetingParticipant> MeetingParticipants { get; set; } = [];

    public ICollection<MeetingTopic> MeetingTopics { get; set; } = [];

    [NotMapped]
    public string DisplayName => $"{Year} - {Location} ({FromDate:yyyy-MM-dd})";
}
