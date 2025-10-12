using System.ComponentModel.DataAnnotations;

namespace DriveHub.ViewModels;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }

    [Required(ErrorMessage = "Please select your role")]
    [Display(Name = "Login As")]
    public string Role { get; set; } = null!; // "Student", "Instructor", "Admin"
}