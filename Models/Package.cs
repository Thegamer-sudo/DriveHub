using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveHub.Models;

public class Package
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Type { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public int LessonCount { get; set; }
    public decimal Price { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    // ADD THESE PROGRESS TRACKING FIELDS:
    public int LessonsCompleted { get; set; } = 0;
    public bool IsDriverReady { get; set; } = false;
    public DateTime? DriverReadyDate { get; set; }
}