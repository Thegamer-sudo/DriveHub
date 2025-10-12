using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveHub.Models;

public class Feedback
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = null!;

    public DateTime SubmittedDate { get; set; } = DateTime.Now;

    public bool IsRead { get; set; } = false;
    public string? AdminResponse { get; set; }
    public DateTime? ResponseDate { get; set; }
}