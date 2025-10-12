using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DriveHub.Models;
using DriveHub.Data;
using DriveHub.ViewModels;
using DriveHub.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace DriveHub.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public PaymentsController(ApplicationDbContext context,
                                 UserManager<ApplicationUser> userManager,
                                 IEmailService emailService,
                                 IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        [Authorize]
        public async Task<IActionResult> Payment(int packageId)
        {
            // FIXED: Get the ACTUAL package from database, not create template
            var package = await _context.Packages
                .FirstOrDefaultAsync(p => p.Id == packageId);

            if (package == null)
            {
                TempData["Error"] = "Package not found. Please select a package first.";
                return RedirectToAction("ChoosePackage", "Packages");
            }

            var model = new PaymentViewModel
            {
                PackageId = packageId,
                PackageType = package.DisplayName,
                Amount = package.Price
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                // Validate card number (exactly 16 digits)
                if (model.CardNumber.Length != 16 || !model.CardNumber.All(char.IsDigit))
                {
                    ModelState.AddModelError("CardNumber", "Card number must be exactly 16 digits");
                    return View("Payment", model);
                }

                // Validate CVV (exactly 3 digits)
                if (model.CVV.Length != 3 || !model.CVV.All(char.IsDigit))
                {
                    ModelState.AddModelError("CVV", "CVV must be exactly 3 digits");
                    return View("Payment", model);
                }

                // FIXED: Get the ACTUAL package from database
                var package = await _context.Packages
                    .FirstOrDefaultAsync(p => p.Id == model.PackageId);

                if (package == null)
                {
                    TempData["Error"] = "Package not found. Please select a package first.";
                    return RedirectToAction("ChoosePackage", "Packages");
                }

                // FIXED: Check if package already has a receipt (already paid)
                var existingReceipt = await _context.Receipts
                    .FirstOrDefaultAsync(r => r.PackageId == package.Id);

                if (existingReceipt != null)
                {
                    TempData["Error"] = $"This package has already been paid. Receipt #: {existingReceipt.ReceiptNumber}";
                    return RedirectToAction("Index", "StudentDashboard");
                }

                // FIXED: Check if user already has an active package of same type
                var existingActivePackage = await _context.Packages
                    .Include(p => p.User)
                    .Where(p => p.UserId == user.Id && p.Type == package.Type)
                    .FirstOrDefaultAsync(p => _context.Receipts.Any(r => r.PackageId == p.Id));

                if (existingActivePackage != null)
                {
                    TempData["Error"] = $"You already have an active {package.Type}. Please continue with your current package.";
                    return RedirectToAction("Index", "StudentDashboard");
                }

                // Create receipt for the EXISTING package (NOT creating new package)
                var receipt = new Receipt
                {
                    UserId = user.Id,
                    PackageId = package.Id, // Use the existing package ID
                    CardHolderName = model.CardHolderName,
                    Last4Digits = model.CardNumber.Substring(model.CardNumber.Length - 4),
                    Expiry = new DateTime(model.ExpiryYear, model.ExpiryMonth, 1),
                    Amount = model.Amount,
                    PaymentDate = DateTime.Now,
                    ReceiptNumber = "RCP" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                };
                _context.Receipts.Add(receipt);
                await _context.SaveChangesAsync();

                // Send payment confirmation email
                try
                {
                    await _emailService.SendReceiptEmailAsync(
                        user.Email ?? "",
                        user.FullName,
                        package.DisplayName,
                        model.Amount,
                        receipt.ReceiptNumber
                    );
                    Console.WriteLine($"✅ Receipt email sent to: {user.Email}");
                }
                catch (Exception emailEx)
                {
                    // Don't fail the payment if email fails, just log it
                    Console.WriteLine($"Email sending failed: {emailEx.Message}");
                }

                TempData["PaymentSuccess"] = $"Payment successful! Receipt #: {receipt.ReceiptNumber}";
                return RedirectToAction("Index", "StudentDashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Payment error: {ex.Message}");
                TempData["PaymentError"] = $"Payment failed: {ex.Message}. Please try again.";
                return View("Payment", model);
            }
        }

        [Authorize]
        public async Task<IActionResult> DebugEmail()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                Console.WriteLine($"=== DEBUG EMAIL STARTED ===");
                Console.WriteLine($"Sending to: {user.Email}");

                // Check if API key exists
                var apiKey = _configuration["SendGrid:ApiKey"];
                Console.WriteLine($"API Key exists: {!string.IsNullOrEmpty(apiKey)}");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine($"API Key length: {apiKey.Length}");
                }

                // Test with a simple email
                await _emailService.SendEmailAsync(user.Email ?? "test@example.com",
                    "DriveHub Debug Test",
                    "This is a debug test email from DriveHub!");

                Console.WriteLine($"=== DEBUG EMAIL COMPLETED ===");
                TempData["Message"] = "Debug email sent - check logs and SendGrid dashboard";
                return RedirectToAction("Index", "StudentDashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== DEBUG EMAIL FAILED: {ex.Message} ===");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                TempData["Error"] = $"Debug email failed: {ex.Message}";
                return RedirectToAction("Index", "StudentDashboard");
            }
        }

        [Authorize]
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                await _emailService.SendEmailAsync(user.Email ?? "",
                    "DriveHub Test Email",
                    "This is a test email from DriveHub! 🚗");

                TempData["Message"] = "Test email sent successfully!";
                return RedirectToAction("Index", "StudentDashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Email test failed: {ex.Message}";
                return RedirectToAction("Index", "StudentDashboard");
            }
        }

        [Authorize]
        public async Task<IActionResult> TestSendGrid()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                // Simple test email
                await _emailService.SendEmailAsync(user.Email ?? "",
                    "SendGrid Test from DriveHub",
                    "This is a test email using SendGrid's official C# library!");

                TempData["Message"] = "SendGrid test email sent! Check your inbox and spam folder.";
                return RedirectToAction("Index", "StudentDashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"SendGrid test failed: {ex.Message}";
                return RedirectToAction("Index", "StudentDashboard");
            }
        }
    }
}