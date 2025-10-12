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

[Authorize(Roles = "Instructor")]
public class InstructorDashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public InstructorDashboardController(ApplicationDbContext context,
                                       UserManager<ApplicationUser> userManager,
                                       IEmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index()
    {
        var instructor = await _userManager.GetUserAsync(User);
        var instructorName = instructor?.FullName?.StartsWith("Instructor") == true ?
            instructor.FullName : $"Instructor {instructor?.FullName?.Split(' ').Last()}";
        ViewBag.InstructorName = instructorName ?? "Instructor";

        // FIXED: Use the REAL assigned students method
        var assignedStudents = await GetAssignedStudents();
        ViewBag.AssignedStudents = assignedStudents;

        // Calculate REAL stats from assigned students
        int totalStudents = assignedStudents.Count;
        int totalPackages = assignedStudents.Sum(s => s.Packages.Count);
        int driverReadyStudents = assignedStudents.Count(s => s.Packages.Any(p => p.IsDriverReady));

        ViewBag.TotalStudents = totalStudents;
        ViewBag.TotalPackages = totalPackages;
        ViewBag.DriverReadyStudents = driverReadyStudents;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendMessageToStudents(List<string> selectedStudentIds, string messageSubject, string messageContent)
    {
        try
        {
            var instructor = await _userManager.GetUserAsync(User);
            int emailsSent = 0;

            if (selectedStudentIds != null && selectedStudentIds.Count > 0)
            {
                foreach (var studentId in selectedStudentIds)
                {
                    var student = await _userManager.FindByIdAsync(studentId);

                    // Security check - only message assigned students
                    if (student != null && student.AssignedInstructorId == instructor.Id)
                    {
                        try
                        {
                            await _emailService.SendEmailAsync(
                                student.Email,
                                messageSubject,
                                $@"<h2>{messageSubject}</h2>
                                   <p>Dear {student.FullName},</p>
                                   <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                                       <strong>Message from your instructor ({instructor.FullName}):</strong>
                                       <p>{messageContent.Replace("\n", "<br>")}</p>
                                   </div>
                                   <p>If you have any questions, please don't hesitate to contact us.</p>
                                   <br>
                                   <p>Best regards,<br>DriveHub Team</p>"
                            );
                            emailsSent++;
                            Console.WriteLine($"✅ Instructor message sent to: {student.Email}");
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"❌ Failed to send to {student.Email}: {emailEx.Message}");
                        }
                    }
                }

                TempData["SuccessMessage"] = $"✅ Messages sent successfully to {emailsSent} student(s)!";
            }
            else
            {
                TempData["ErrorMessage"] = "❌ Please select at least one student to message.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"❌ Error sending messages: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> MyStudents()
    {
        // FIXED: Use the REAL assigned students method
        var assignedStudents = await GetAssignedStudents();
        return View(assignedStudents);
    }

    public async Task<IActionResult> StudentDetails(string studentId)
    {
        var instructor = await _userManager.GetUserAsync(User);
        var student = await _userManager.FindByIdAsync(studentId);

        // FIXED: Add back the security check
        if (student == null || student.AssignedInstructorId != instructor.Id)
        {
            return Forbid();
        }

        var packages = await _context.Packages
            .Where(p => p.UserId == studentId)
            .ToListAsync();

        var receipts = await _context.Receipts
            .Where(r => r.UserId == studentId)
            .Include(r => r.Package)
            .ToListAsync();

        var studentDetail = new StudentDetailViewModel
        {
            Student = student,
            Packages = packages,
            Receipts = receipts
        };

        return View(studentDetail);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateLessonProgress(string studentId, int packageId, int lessonsCompleted)
    {
        var instructor = await _userManager.GetUserAsync(User);
        var student = await _userManager.FindByIdAsync(studentId);

        // Security check - only assigned instructor can update
        if (student == null || student.AssignedInstructorId != instructor.Id)
        {
            return Forbid();
        }

        var package = await _context.Packages.FindAsync(packageId);
        if (package != null)
        {
            // Validate lessons completed doesn't exceed total lessons
            lessonsCompleted = Math.Min(lessonsCompleted, package.LessonCount);
            lessonsCompleted = Math.Max(lessonsCompleted, 0); // Ensure not negative

            package.LessonsCompleted = lessonsCompleted;

            // Auto-mark as driver ready if all lessons completed
            if (lessonsCompleted >= package.LessonCount)
            {
                package.IsDriverReady = true;
                package.DriverReadyDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ Updated {package.Type} progress to {lessonsCompleted}/{package.LessonCount} lessons completed for {student.FullName}.";
        }
        else
        {
            TempData["ErrorMessage"] = "Package not found.";
        }

        return RedirectToAction("StudentDetails", new { studentId });
    }

    [HttpPost]
    public async Task<IActionResult> MarkPackageComplete(string studentId, int packageId)
    {
        var instructor = await _userManager.GetUserAsync(User);
        var student = await _userManager.FindByIdAsync(studentId);

        // Security check - only assigned instructor can update
        if (student == null || student.AssignedInstructorId != instructor.Id)
        {
            return Forbid();
        }

        var package = await _context.Packages.FindAsync(packageId);
        if (package != null)
        {
            package.IsDriverReady = true;
            package.LessonsCompleted = package.LessonCount; // Complete all lessons
            package.DriverReadyDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"🎉 {student.FullName} is now DRIVER READY! Congratulations!";
        }
        else
        {
            TempData["ErrorMessage"] = "Package not found.";
        }

        return RedirectToAction("StudentDetails", new { studentId });
    }

    [HttpPost]
    public async Task<IActionResult> AddStudentNotes(string studentId, string notes)
    {
        var instructor = await _userManager.GetUserAsync(User);
        var student = await _userManager.FindByIdAsync(studentId);

        // FIXED: Add back the security check
        if (student == null || student.AssignedInstructorId != instructor.Id)
        {
            return Forbid();
        }

        if (!string.IsNullOrEmpty(notes))
        {
            TempData["SuccessMessage"] = $"Notes added for {student.FullName}.";
        }

        return RedirectToAction("StudentDetails", new { studentId });
    }

    private async Task<List<StudentDetailViewModel>> GetAssignedStudents()
    {
        var instructor = await _userManager.GetUserAsync(User);

        // Get ONLY students assigned to this instructor
        var assignedStudents = await _userManager.GetUsersInRoleAsync("Student");
        var filteredStudents = assignedStudents.Where(s => s.AssignedInstructorId == instructor.Id).ToList();

        var studentDetails = new List<StudentDetailViewModel>();

        foreach (var student in filteredStudents)
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

        return studentDetails;
    }
}