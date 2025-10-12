using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveHub.Data;
using DriveHub.Models;
using System.Security.Claims;
using DriveHub.Services;

namespace DriveHub.Controllers;

public class PackagesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PackagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize]
    public async Task<IActionResult> ChoosePackage(bool? upgrade)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Calculate age
        var isUnder18 = true;
        var userAge = 0;

        try
        {
            // Try using ID Number first
            if (!string.IsNullOrEmpty(user.IDNumber))
            {
                isUnder18 = !IdNumberHelper.Is18OrOver(user.IDNumber);
                userAge = CalculateAgeFromIDNumber(user.IDNumber);
            }
            else if (user.DateOfBirth != default(DateTime))
            {
                userAge = CalculateAge(user.DateOfBirth);
                isUnder18 = userAge < 18;
            }
            else
            {
                isUnder18 = true;
                userAge = 16;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Age calculation error: {ex.Message}");
            if (user.DateOfBirth != default(DateTime))
            {
                userAge = CalculateAge(user.DateOfBirth);
                isUnder18 = userAge < 18;
            }
            else
            {
                isUnder18 = true;
                userAge = 16;
            }
        }

        ViewBag.UserAge = userAge;
        ViewBag.IsUnder18 = isUnder18;

        // Check if this is an upgrade flow
        if (upgrade == true)
        {
            var completedLearners = await _context.Packages
                .FirstOrDefaultAsync(p => p.UserId == user.Id &&
                                   p.Type.ToLower().Contains("learners") &&
                                   p.IsDriverReady);

            if (completedLearners != null)
            {
                ViewBag.IsUpgradeFlow = true;
                ViewBag.CompletedLearnersPackage = completedLearners;

                // NEW: For under 18 who completed Learners, show special message
                if (isUnder18)
                {
                    ViewBag.Under18CompletedLearners = true;
                }
            }
            else
            {
                return RedirectToAction("ChoosePackage");
            }
        }
        else
        {
            ViewBag.IsUpgradeFlow = false;
            ViewBag.Under18CompletedLearners = false;
        }

        return View(GetPackageTemplates());
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SelectPackage(int packageId, string? selectedCode = null)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Get package template
            var packageTemplate = GetPackageTemplate(packageId, selectedCode);
            if (packageTemplate == null)
            {
                TempData["Error"] = "Invalid package selected.";
                return RedirectToAction("ChoosePackage");
            }

            // Age validation
            var isUnder18 = true;
            try
            {
                if (!string.IsNullOrEmpty(user.IDNumber))
                {
                    isUnder18 = !IdNumberHelper.Is18OrOver(user.IDNumber);
                }
                else if (user.DateOfBirth != default(DateTime))
                {
                    var age = CalculateAge(user.DateOfBirth);
                    isUnder18 = age < 18;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Age validation error: {ex.Message}");
                isUnder18 = true;
            }

            // NEW: Special case - under 18 who completed Learners cannot upgrade
            var completedLearners = await _context.Packages
                .FirstOrDefaultAsync(p => p.UserId == user.Id &&
                                   p.Type.ToLower().Contains("learners") &&
                                   p.IsDriverReady);

            if (isUnder18 && completedLearners != null)
            {
                TempData["Error"] = "You have completed Learners training. Please come back when you turn 18 to continue with Drivers training.";
                return RedirectToAction("Index", "StudentDashboard");
            }

            if ((packageId == 2 || packageId == 3 || packageId == 4 || packageId == 5) && isUnder18)
            {
                TempData["Error"] = "You must be 18 years or older to select driver's packages.";
                return RedirectToAction("ChoosePackage");
            }

            // FIXED: ULTIMATE DUPLICATE PREVENTION - Check for ANY package of same type
            var existingPackageOfSameType = await _context.Packages
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type == packageTemplate.Type);

            if (existingPackageOfSameType != null)
            {
                // ALWAYS redirect to payment for existing package (don't create new one)
                TempData["InfoMessage"] = $"You already have a {packageTemplate.Type}. Redirecting to payment...";
                return RedirectToAction("Payment", "Payments", new { packageId = existingPackageOfSameType.Id });
            }

            // FIXED: Additional duplicate prevention for specific package categories
            if (packageTemplate.Type.Contains("Drivers"))
            {
                var existingDrivers = await _context.Packages
                    .FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type.Contains("Drivers"));

                if (existingDrivers != null)
                {
                    TempData["InfoMessage"] = $"You already have a Drivers package. Redirecting to payment...";
                    return RedirectToAction("Payment", "Payments", new { packageId = existingDrivers.Id });
                }
            }

            if (packageTemplate.Type.Contains("Full"))
            {
                var existingFull = await _context.Packages
                    .FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type.Contains("Full"));

                if (existingFull != null)
                {
                    TempData["InfoMessage"] = $"You already have a Full package. Redirecting to payment...";
                    return RedirectToAction("Payment", "Payments", new { packageId = existingFull.Id });
                }
            }

            // Business logic validations
            if (completedLearners != null && packageId == 1)
            {
                TempData["Error"] = "You have already completed the Learners package. Please upgrade to Drivers training.";
                return RedirectToAction("ChoosePackage", new { upgrade = true });
            }

            if (completedLearners != null && (packageId == 4 || packageId == 5))
            {
                TempData["Error"] = "You have already completed the Learners phase. Please select a Drivers package instead.";
                return RedirectToAction("ChoosePackage", new { upgrade = true });
            }

            // FIXED: Create new package ONLY if no duplicates found
            var userPackage = new Package
            {
                Type = packageTemplate.Type,
                DisplayName = packageTemplate.DisplayName,
                LessonCount = packageTemplate.LessonCount,
                Price = packageTemplate.Price,
                UserId = user.Id,
                LessonsCompleted = 0,
                IsDriverReady = false
            };

            _context.Packages.Add(userPackage);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ {packageTemplate.DisplayName} selected successfully!";
            return RedirectToAction("Payment", "Payments", new { packageId = userPackage.Id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Package selection error: {ex.Message}");
            TempData["Error"] = "Failed to select package. Please try again.";
            return RedirectToAction("ChoosePackage");
        }
    }

    // Helper method to calculate age from DateOfBirth
    private int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }

    // Helper method to calculate age from ID Number
    private int CalculateAgeFromIDNumber(string idNumber)
    {
        try
        {
            if (idNumber.Length >= 6)
            {
                var yearStr = idNumber.Substring(0, 2);
                var monthStr = idNumber.Substring(2, 2);
                var dayStr = idNumber.Substring(4, 2);

                if (int.TryParse(yearStr, out int year) &&
                    int.TryParse(monthStr, out int month) &&
                    int.TryParse(dayStr, out int day))
                {
                    year = year <= 21 ? 2000 + year : 1900 + year;
                    var birthDate = new DateTime(year, month, day);
                    return CalculateAge(birthDate);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating age from ID: {ex.Message}");
        }
        return 16;
    }

    private List<Package> GetPackageTemplates()
    {
        return new List<Package>
        {
            new Package { Id = 1, Type = "Learners Package", DisplayName = "Learners Package", LessonCount = 20, Price = 500.00m },
            new Package { Id = 2, Type = "Drivers Package", DisplayName = "Drivers Package - Code 8", LessonCount = 30, Price = 2000.00m },
            new Package { Id = 3, Type = "Drivers Package", DisplayName = "Drivers Package - Code 10", LessonCount = 30, Price = 3000.00m },
            new Package { Id = 4, Type = "Full Package", DisplayName = "Full Package - Learners + Code 8", LessonCount = 50, Price = 2000.00m },
            new Package { Id = 5, Type = "Full Package", DisplayName = "Full Package - Learners + Code 10", LessonCount = 50, Price = 2500.00m }
        };
    }

    private Package GetPackageTemplate(int packageId, string? selectedCode = null)
    {
        return packageId switch
        {
            1 => new Package { Type = "Learners Package", DisplayName = "Learners Package", LessonCount = 20, Price = 500.00m },
            2 => new Package { Type = "Drivers Package", DisplayName = "Drivers Package - Code 8", LessonCount = 30, Price = 2000.00m },
            3 => new Package { Type = "Drivers Package", DisplayName = "Drivers Package - Code 10", LessonCount = 30, Price = 3000.00m },
            4 => new Package { Type = "Full Package", DisplayName = "Full Package - Learners + Code 8", LessonCount = 50, Price = 2000.00m },
            5 => new Package { Type = "Full Package", DisplayName = "Full Package - Learners + Code 10", LessonCount = 50, Price = 2500.00m },
            _ => new Package()
        };
    }
}