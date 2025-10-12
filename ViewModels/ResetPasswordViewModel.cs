using System.ComponentModel.DataAnnotations;

namespace DriveHub.ViewModels;

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = null!;

    public string Token { get; set; } = null!;
}