using DriveHub.Models;

namespace DriveHub.ViewModels;

public class StudentDetailViewModel
{
    public ApplicationUser Student { get; set; } = null!;
    public List<Package> Packages { get; set; } = new();
    public List<Receipt> Receipts { get; set; } = new();

    // ADD THESE PROPERTIES FOR ASSIGNMENT MANAGEMENT
    public ApplicationUser? AssignedInstructor { get; set; }
    public string AssignedInstructorName => AssignedInstructor?.FullName ?? "Not Assigned";
}