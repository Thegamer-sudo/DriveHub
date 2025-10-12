using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DriveHub.Models;
using DriveHub.ViewModels;
using DriveHub.Services;
using Microsoft.AspNetCore.Authorization;
using System.Net;

namespace DriveHub.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;

    public AccountController(UserManager<ApplicationUser> userManager,
                           SignInManager<ApplicationUser> signInManager,
                           IEmailService emailService,
                           ISmsService smsService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _smsService = smsService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Validate ID Number and calculate age
            try
            {
                var (age, dateOfBirth) = IdNumberHelper.CalculateAgeFromId(model.IDNumber);

                // Age validation
                if (age < 16)
                {
                    ModelState.AddModelError(string.Empty, "You must be at least 16 years old to register.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    IDNumber = model.IDNumber,
                    Address = model.Address,
                    DateOfBirth = dateOfBirth,
                    PhoneNumber = model.PhoneNumber
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Student");
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // SEND WELCOME EMAIL
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
                        Console.WriteLine($"✅ Welcome email sent to: {user.Email}");
                    }
                    catch (Exception ex)
                    {
                        // Log email failure but don't break registration
                        Console.WriteLine($"❌ Welcome email failed: {ex.Message}");
                    }

                    // ✅ NEW: SEND WELCOME SMS
                    try
                    {
                        if (!string.IsNullOrEmpty(model.PhoneNumber))
                        {
                            var smsMessage = $"Welcome to DriveHub {user.FullName}! Your account was created successfully. Check email for details. 🚗";
                            var smsSent = await _smsService.SendSMSAsync(model.PhoneNumber, smsMessage);

                            if (smsSent)
                            {
                                Console.WriteLine($"✅ Welcome SMS sent to: {model.PhoneNumber}");
                            }
                            else
                            {
                                Console.WriteLine($"❌ Welcome SMS failed for: {model.PhoneNumber}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("ℹ️ No phone number provided for welcome SMS");
                        }
                    }
                    catch (Exception smsEx)
                    {
                        Console.WriteLine($"❌ Welcome SMS failed: {smsEx.Message}");
                        // Don't break registration if SMS fails
                    }

                    TempData["SuccessMessage"] = "Registration successful! Welcome to DriveHub.";
                    return RedirectToAction("Index", "StudentDashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                // Clean error message - no technical details
                ModelState.AddModelError("IDNumber", "Invalid ID number");
                Console.WriteLine($"ID Validation Error: {ex.Message}");
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            // FIRST: Find the user and check role BEFORE signing in
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // Check if user has the selected role BEFORE authentication
                var isInSelectedRole = await _userManager.IsInRoleAsync(user, model.Role);
                if (!isInSelectedRole)
                {
                    // Show generic error without ever signing in
                    ModelState.AddModelError(string.Empty, "Invalid role selection. Please login with the correct role.");
                    return View(model);
                }

                // ONLY if role is correct, attempt to sign in
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    // Redirect based on actual role
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "AdminDashboard");
                    }
                    else if (await _userManager.IsInRoleAsync(user, "Instructor"))
                    {
                        return RedirectToAction("Index", "InstructorDashboard");
                    }
                    else
                    {
                        return RedirectToAction("Index", "StudentDashboard");
                    }
                }
            }

            // If we get here, either user not found or password wrong
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            Console.WriteLine($"=== FORGOT PASSWORD STARTED ===");
            Console.WriteLine($"Email: {model.Email}");

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                Console.WriteLine($"✅ User found: {user.Email}");

                // REMOVED: Email confirmation check - allow password reset even if email not confirmed
                // var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
                // Console.WriteLine($"Email confirmed: {isEmailConfirmed}");

                // ALWAYS allow password reset, regardless of email confirmation status
                try
                {
                    // Generate reset token
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    Console.WriteLine($"✅ Token generated: {!string.IsNullOrEmpty(token)}");

                    // URL encode the token for safety
                    var encodedToken = WebUtility.UrlEncode(token);

                    // Create reset URL
                    var resetUrl = Url.Action("ResetPassword", "Account",
                        new { token = encodedToken, email = model.Email },
                        protocol: HttpContext.Request.Scheme);

                    Console.WriteLine($"Reset URL: {resetUrl}");

                    if (!string.IsNullOrEmpty(resetUrl))
                    {
                        // Send reset email
                        await _emailService.SendPasswordResetAsync(model.Email, resetUrl);
                        Console.WriteLine($"✅ Password reset email sent to: {model.Email}");

                        // Log email details for debugging
                        Console.WriteLine($"Email service type: {_emailService.GetType().Name}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Reset URL is null or empty");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during password reset: {ex.Message}");
                    Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"❌ User not found: {model.Email}");
            }

            Console.WriteLine($"=== FORGOT PASSWORD COMPLETED ===");

            // Always return confirmation (security best practice)
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        return View(model);
    }

    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string token, string email)
    {
        var model = new ResetPasswordViewModel { Token = token, Email = email };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("ResetPasswordConfirmation");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }
        return View(model);
    }

    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    // Helper method to get user role
    private async Task<string> GetUserRole(ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, "Admin")) return "Admin";
        if (await _userManager.IsInRoleAsync(user, "Instructor")) return "Instructor";
        if (await _userManager.IsInRoleAsync(user, "Student")) return "Student";
        return "Unknown";
    }
}