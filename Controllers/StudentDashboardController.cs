using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveHub.Data;
using DriveHub.Models;
using DriveHub.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using DriveHub.Services;

namespace DriveHub.Controllers;

[Authorize(Roles = "Student")]
public class StudentDashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        // Check for payment success message
        if (TempData["PaymentSuccess"] != null)
        {
            ViewBag.PaymentSuccess = TempData["PaymentSuccess"];
        }

        var student = await _userManager.GetUserAsync(User);
        if (student == null)
        {
            return RedirectToAction("Login", "Account");
        }

        ViewBag.StudentName = student.FullName ?? "Student";

        try
        {
            // Get student's packages and receipts
            var packages = await _context.Packages
                .Where(p => p.UserId == student.Id)
                .ToListAsync();

            var receipts = await _context.Receipts
                .Where(r => r.UserId == student.Id)
                .Include(r => r.Package)
                .OrderByDescending(r => r.PaymentDate)
                .ToListAsync();

            // FIXED: Check if student completed Learners and can upgrade
            var completedLearnersPackage = packages.FirstOrDefault(p =>
                p.Type.ToLower().Contains("learners") &&
                p.IsDriverReady &&
                !p.Type.ToLower().Contains("full"));

            // FIXED: Check if student already has a Drivers package (don't show upgrade if they do)
            var hasDriversPackage = packages.Any(p =>
                p.Type.ToLower().Contains("drivers"));

            // Only show upgrade if they completed Learners AND don't have Drivers package yet
            ViewBag.CanUpgradeToDrivers = completedLearnersPackage != null && !hasDriversPackage;
            ViewBag.CompletedLearnersPackage = completedLearnersPackage;

            // Check payment status for each package
            var packageStatuses = new List<PackageStatusViewModel>();
            foreach (var package in packages)
            {
                var hasPaid = receipts.Any(r => r.PackageId == package.Id);
                var progressPercentage = package.LessonCount > 0 ?
                    (package.LessonsCompleted * 100) / package.LessonCount : 0;

                packageStatuses.Add(new PackageStatusViewModel
                {
                    Package = package,
                    HasPaid = hasPaid,
                    Receipt = receipts.FirstOrDefault(r => r.PackageId == package.Id),
                    ProgressPercentage = progressPercentage,
                    IsDriverReady = package.IsDriverReady
                });
            }

            // Get ACTUAL assigned instructor
            var assignedInstructor = await _userManager.FindByIdAsync(student.AssignedInstructorId);

            ViewBag.PackageStatuses = packageStatuses;
            ViewBag.Receipts = receipts;
            ViewBag.AssignedInstructor = assignedInstructor;

            // Driving tips
            ViewBag.DrivingTips = new List<string>
            {
                "Always check your mirrors before changing lanes or turning.",
                "Maintain a safe following distance of at least 3 seconds.",
                "Use your indicators at least 5 seconds before turning.",
                "Keep both hands on the steering wheel at the 9 and 3 o'clock positions.",
                "Adjust your seat and mirrors before starting the engine.",
                "Always wear your seatbelt - it's the law and it saves lives.",
                "Be aware of blind spots when changing lanes.",
                "Slow down in wet weather and increase your following distance."
            };

            return View();
        }
        catch (Exception ex)
        {
            ViewBag.PackageStatuses = new List<PackageStatusViewModel>();
            ViewBag.Receipts = new List<Receipt>();
            ViewBag.AssignedInstructor = null;
            ViewBag.CanUpgradeToDrivers = false;
            ViewBag.CompletedLearnersPackage = null;
            ViewBag.DrivingTips = new List<string>
            {
                "Welcome to DriveHub! Please complete your package selection to get started."
            };

            Console.WriteLine($"Database error: {ex.Message}");
            return View();
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitFeedback(string feedbackText)
    {
        var student = await _userManager.GetUserAsync(User);

        if (student == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!string.IsNullOrEmpty(feedbackText))
        {
            var feedback = new Feedback
            {
                UserId = student.Id,
                Message = feedbackText,
                SubmittedDate = DateTime.Now,
                IsRead = false
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["FeedbackMessage"] = "✅ Thank you for your feedback! We'll get back to you soon.";
        }
        else
        {
            TempData["FeedbackMessage"] = "❌ Please enter your feedback before submitting.";
        }

        return RedirectToAction("Index");
    }

    // ADD THESE METHODS FOR VEHICLE BOOKING

    public IActionResult BookVehicle()
    {
        var student = _userManager.GetUserAsync(User).Result;
        if (student == null) return RedirectToAction("Login", "Account");

        // Check if student is eligible to book a vehicle
        var (isEligible, message) = IsStudentEligibleForVehicleBooking(student.Id);

        if (!isEligible)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction("Index");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> BookVehicle(DateTime testDate, string testLocation, string vehicleType, string specialRequirements)
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null) return RedirectToAction("Login", "Account");

        // Check eligibility again
        var (isEligible, message) = IsStudentEligibleForVehicleBooking(student.Id);
        if (!isEligible)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction("Index");
        }

        try
        {
            var booking = new VehicleBooking
            {
                UserId = student.Id,
                TestDate = testDate,
                TestLocation = testLocation,
                VehicleType = vehicleType,
                SpecialRequirements = specialRequirements,
                Status = "Pending",
                BookingFee = 250.00m
            };

            _context.VehicleBookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "✅ Vehicle booking request submitted! We'll confirm your booking within 24 hours.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error submitting booking: {ex.Message}";
            return View();
        }
    }

    public async Task<IActionResult> MyBookings()
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null) return RedirectToAction("Login", "Account");

        var bookings = await _context.VehicleBookings
            .Where(b => b.UserId == student.Id)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return View(bookings);
    }

    // UPDATED: Helper method to check eligibility using ID Number
    private (bool isEligible, string message) IsStudentEligibleForVehicleBooking(string studentId)
    {
        var student = _userManager.FindByIdAsync(studentId).Result;
        if (student == null)
        {
            return (false, "Student not found.");
        }

        var packages = _context.Packages.Where(p => p.UserId == studentId).ToList();
        var driverReadyPackage = packages.FirstOrDefault(p => p.IsDriverReady);

        if (driverReadyPackage == null)
        {
            return (false, "❌ You must complete your driving lessons and be marked as 'Driver Ready' by your instructor before booking a vehicle.");
        }

        // Check if the package is appropriate (not learners only)
        if (driverReadyPackage.Type.ToLower().Contains("learners") && !driverReadyPackage.Type.ToLower().Contains("full"))
        {
            return (false, "❌ Vehicle booking is only available for students with Drivers or Full packages. Please upgrade your package.");
        }

        // UPDATED: Check age using ID number (must be 18+ for drivers test)
        try
        {
            var is18OrOver = IdNumberHelper.Is18OrOver(student.IDNumber);
            if (!is18OrOver)
            {
                return (false, "❌ You must be 18 years or older to book a vehicle for the driving test.");
            }
        }
        catch (Exception ex)
        {
            return (false, $"❌ Error validating age from ID number: {ex.Message}");
        }

        return (true, "Eligible to book vehicle");
    }
}