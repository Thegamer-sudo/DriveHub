using Microsoft.AspNetCore.Identity;

namespace DriveHub.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = null!;


    public DateTime DateOfBirth { get; set; }
    public string IDNumber { get; set; } = null!;
    public string Address { get; set; } = null!;

    // UNCOMMENT THESE - We're adding them back properly
    public string? AssignedInstructorId { get; set; }
    
    // Navigation property for the assigned instructor
    public virtual ApplicationUser? AssignedInstructor { get; set; }

    public virtual ICollection<Receipt>? Receipts { get; set; }
}