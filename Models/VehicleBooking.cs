using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveHub.Models;

public class VehicleBooking
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    [Required]
    public DateTime TestDate { get; set; }

    [Required]
    public string TestLocation { get; set; } = null!;

    [Required]
    public string VehicleType { get; set; } = null!; // Car, Truck, etc.

    public string? SpecialRequirements { get; set; }

    public DateTime BookingDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled

    public decimal BookingFee { get; set; } = 250.00m; // Standard booking fee
}