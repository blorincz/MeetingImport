using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilderbergImport.Models;

public class MeetingParticipant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int MeetingId { get; set; }

    [Required]
    public int ParticipantId { get; set; }

    [ForeignKey("MeetingId")]
    public Meeting Meeting { get; set; } = null!;

    [ForeignKey("ParticipantId")]
    public Participant Participant { get; set; } = null!;
}
