using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilderbergImport.Models;

public class MeetingParticipant
{
    [Key, Column(Order = 0)]
    public int MeetingId { get; set; }

    [Key, Column(Order = 1)]
    public int ParticipantId { get; set; }

    [ForeignKey("MeetingId")]
    public Meeting Meeting { get; set; } = null!;

    [ForeignKey("ParticipantId")]
    public Participant Participant { get; set; } = null!;
}
