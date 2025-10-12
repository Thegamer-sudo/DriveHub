using DriveHub.Models;

namespace DriveHub.ViewModels;

public class PackageStatusViewModel
{
    public Package Package { get; set; } = null!;
    public bool HasPaid { get; set; }
    public Receipt? Receipt { get; set; }

    public int ProgressPercentage { get; set; }
    public bool IsDriverReady { get; set; }
    // Helper properties for display
    public string DisplayName => GetPackageDisplayName(Package);
    public string Description => GetPackageDescription(Package);

    private string GetPackageDescription(Package package)
    {
        return package.Type switch
        {
            "Learners" => "20 comprehensive lessons for learner's license",
            "Drivers-Code8" => "30 professional driving lessons for Code 8 license",
            "Drivers-Code10" => "30 comprehensive lessons for Code 10 license",
            "Full-Code8" => "Complete training: Learners + Drivers Code 8",
            "Full-Code10" => "Complete training: Learners + Drivers Code 10",
            _ => "Driving lessons package"
        };
    }

    private string GetPackageDisplayName(Package package)
    {
        return package.Type switch
        {
            "Learners" => "Learners Package",
            "Drivers-Code8" => "Drivers Package - Code 8",
            "Drivers-Code10" => "Drivers Package - Code 10",
            "Full-Code8" => "Full Package - Code 8",
            "Full-Code10" => "Full Package - Code 10",
            _ => "Driving Package"
        };
    }
}