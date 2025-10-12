using System.ComponentModel.DataAnnotations;

namespace DriveHub.ViewModels;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}