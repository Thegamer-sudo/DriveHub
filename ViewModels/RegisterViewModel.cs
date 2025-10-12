using System.ComponentModel.DataAnnotations;

namespace DriveHub.ViewModels;

public class RegisterViewModel
{
    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = null!;

    [Required]
    [Display(Name = "ID Number")]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "Invalid ID number")]
    [RegularExpression(@"^\d{13}$", ErrorMessage = "Invalid ID number")]
    public string IDNumber { get; set; } = null!;

    [Required]
    [Display(Name = "Address")]
    public string Address { get; set; } = null!;

    [Required]
    [Display(Name = "Phone Number")]
    [Phone(ErrorMessage = "Please enter a valid phone number")]
    [RegularExpression(@"^(\+27|0)[1-8][0-9]{8}$", ErrorMessage = "Please enter a valid South African phone number")]
    public string PhoneNumber { get; set; } = null!;
}