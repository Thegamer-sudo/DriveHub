using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DriveHub.Data;
using DriveHub.Models;
using DriveHub.Services;
using DriveHub.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace DriveHub.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public AdminDashboardController(ApplicationDbContext context,
                                  UserManager<ApplicationUser> userManager,
                                  IEmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
    }

    // UNCOMMENT AND FIX THIS SECTION - Manage Assignments
    public async Task<IActionResult> ManageAssignments()
    {
        var students = await _userManager.GetUsersInRoleAsync("Student");
        var instructors = await _userManager.GetUsersInRoleAsync("Instructor");

        var studentDetails = new List<StudentDetailViewModel>();

        foreach (var student in students)
        {
            // Get assigned instructor if exists
            ApplicationUser? assignedInstructor = null;
            if (!string.IsNullOrEmpty(student.AssignedInstructorId))
            {
                assignedInstructor = await _userManager.FindByIdAsync(student.AssignedInstructorId);
            }

            studentDetails.Add(new StudentDetailViewModel
            {
                Student = student,
                AssignedInstructor = assignedInstructor,
                Packages = new List<Package>(),
                Receipts = new List<Receipt>()
            });
        }

        ViewBag.Instructors = instructors;
        return View(studentDetails);
    }

    // Add these methods to your existing AdminDashboardController

    public async Task<IActionResult> Feedback()
    {
        var feedbacks = await _context.Feedbacks
            .Include(f => f.User)
            .OrderByDescending(f => f.SubmittedDate)
            .ToListAsync();

        return View(feedbacks);
    }


    [HttpPost]
    public async Task<IActionResult> RespondToFeedback(int feedbackId, string adminResponse)
    {
        try
        {
            var feedback = await _context.Feedbacks
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback != null && !string.IsNullOrEmpty(adminResponse))
            {
                feedback.AdminResponse = adminResponse;
                feedback.ResponseDate = DateTime.Now;
                feedback.IsRead = true;
                await _context.SaveChangesAsync();

                // SEND EMAIL TO STUDENT
                try
                {
                    await _emailService.SendEmailAsync(
                        feedback.User.Email,
                        "Response to Your Feedback - DriveHub",
                        $@"<h2>Response to Your Feedback</h2>
                       <p>Dear {feedback.User.FullName},</p>
                       <p>Thank you for your feedback. Here is our response:</p>
                       <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                           <strong>Admin Response:</strong>
                           <p>{adminResponse}</p>
                       </div>
                       <p>Best regards,<br>DriveHub Team</p>"
                    );
                    Console.WriteLine($"✅ Response email sent to: {feedback.User.Email}");
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"❌ Response email failed: {emailEx.Message}");
                    // Don't fail the whole operation if email fails
                }

                TempData["SuccessMessage"] = "✅ Response sent successfully!";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
        }
        return RedirectToAction("Feedback");
    }

    [HttpPost]
    public async Task<IActionResult> AssignInstructor(string studentId, string instructorId)
    {
        try
        {
            var student = await _userManager.FindByIdAsync(studentId);
            var instructor = await _userManager.FindByIdAsync(instructorId);

            if (student != null && instructor != null)
            {
                student.AssignedInstructorId = instructorId;
                await _userManager.UpdateAsync(student);

                TempData["SuccessMessage"] = $"✅ Successfully assigned {instructor.FullName} to {student.FullName}";
            }
            else
            {
                TempData["ErrorMessage"] = "❌ Student or instructor not found";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"❌ Error assigning instructor: {ex.Message}";
        }

        return RedirectToAction("ManageAssignments");
    }

    [HttpPost]
    public async Task<IActionResult> RemoveAssignment(string studentId)
    {
        try
        {
            var student = await _userManager.FindByIdAsync(studentId);
            if (student != null)
            {
                student.AssignedInstructorId = null;
                await _userManager.UpdateAsync(student);
                TempData["SuccessMessage"] = $"✅ Successfully removed assignment for {student.FullName}";
            }
            else
            {
                TempData["ErrorMessage"] = "❌ Student not found";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"❌ Error removing assignment: {ex.Message}";
        }

        return RedirectToAction("ManageAssignments");
    }

    public async Task<IActionResult> Index()
    {
        var admin = await _userManager.GetUserAsync(User);
        ViewBag.AdminName = admin?.FullName ?? "Admin";

        // Get stats for dashboard - fixed for SQLite
        var studentCount = await _userManager.GetUsersInRoleAsync("Student");
        var instructorCount = await _userManager.GetUsersInRoleAsync("Instructor");

        // Fix for SQLite: Get all receipts and calculate sum on client side
        var allReceipts = await _context.Receipts.ToListAsync();
        var totalRevenue = allReceipts.Sum(r => r.Amount);

        var recentPayments = await _context.Receipts
            .Include(r => r.User)
            .Include(r => r.Package)
            .OrderByDescending(r => r.PaymentDate)
            .Take(5)
            .ToListAsync();

        // TEMPORARY - Remove assignment stats until fixed
        // var assignedStudentsCount = studentCount.Count(s => !string.IsNullOrEmpty(s.AssignedInstructorId));

        ViewBag.StudentCount = studentCount.Count;
        ViewBag.InstructorCount = instructorCount.Count;
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.RecentPayments = recentPayments;
        ViewBag.SentEmailsCount = _emailService.SentEmails;
        // ViewBag.AssignedStudentsCount = assignedStudentsCount;
        // ViewBag.UnassignedStudentsCount = studentCount.Count - assignedStudentsCount;

        return View();
    }

    public async Task<IActionResult> Students()
    {
        var students = await _userManager.GetUsersInRoleAsync("Student");
        var studentDetails = new List<StudentDetailViewModel>();

        foreach (var student in students)
        {
            var packages = await _context.Packages
                .Where(p => p.UserId == student.Id)
                .ToListAsync();

            var receipts = await _context.Receipts
                .Where(r => r.UserId == student.Id)
                .Include(r => r.Package)
                .ToListAsync();

            studentDetails.Add(new StudentDetailViewModel
            {
                Student = student,
                Packages = packages,
                Receipts = receipts
            });
        }

        return View(studentDetails);
    }

    public async Task<IActionResult> Payments()
    {
        var receipts = await _context.Receipts
            .Include(r => r.User)
            .Include(r => r.Package)
            .OrderByDescending(r => r.PaymentDate)
            .ToListAsync();

        return View(receipts);
    }

    public IActionResult EmailLog()
    {
        ViewBag.SentEmailsCount = _emailService.SentEmails;
        return View();
    }

    public async Task<IActionResult> Instructors()
    {
        var instructors = await _userManager.GetUsersInRoleAsync("Instructor");
        return View(instructors);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateInstructorPhone(string instructorId, string phoneNumber)
    {
        try
        {
            var instructor = await _userManager.FindByIdAsync(instructorId);
            if (instructor != null)
            {
                instructor.PhoneNumber = phoneNumber;
                await _userManager.UpdateAsync(instructor);
                TempData["SuccessMessage"] = $"✅ Phone number updated for {instructor.FullName}";
            }
            else
            {
                TempData["ErrorMessage"] = "❌ Instructor not found";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"❌ Error updating phone: {ex.Message}";
        }

        return RedirectToAction("Instructors");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStudent(string studentId)
    {
        var student = await _userManager.FindByIdAsync(studentId);
        if (student == null)
        {
            TempData["ErrorMessage"] = "Student not found.";
            return RedirectToAction("Students");
        }

        try
        {
            var packages = await _context.Packages.Where(p => p.UserId == studentId).ToListAsync();
            var receipts = await _context.Receipts.Where(r => r.UserId == studentId).ToListAsync();

            _context.Packages.RemoveRange(packages);
            _context.Receipts.RemoveRange(receipts);
            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(student);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Student {student.FullName} has been archived successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete student account.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting student: {ex.Message}";
        }

        return RedirectToAction("Students");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTestStudents()
    {
        try
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var deletedCount = 0;

            foreach (var student in students)
            {
                try
                {
                    var packages = await _context.Packages.Where(p => p.UserId == student.Id).ToListAsync();
                    var receipts = await _context.Receipts.Where(r => r.UserId == student.Id).ToListAsync();

                    _context.Packages.RemoveRange(packages);
                    _context.Receipts.RemoveRange(receipts);
                    await _context.SaveChangesAsync();

                    var result = await _userManager.DeleteAsync(student);
                    if (result.Succeeded)
                    {
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting student {student.FullName}: {ex.Message}");
                }
            }

            TempData["SuccessMessage"] = $"Successfully archived {deletedCount} student accounts.";
            return RedirectToAction("Students");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting students: {ex.Message}";
            return RedirectToAction("Students");
        }
    }
}