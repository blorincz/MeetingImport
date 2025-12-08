using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BilderbergImport.Models;

public class MeetingTopic
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int MeetingId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    [ForeignKey("MeetingId")]
    public Meeting Meeting { get; set; } = null!;
}
