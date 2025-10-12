using System.Diagnostics;
using DriveHub.Models;
using Microsoft.AspNetCore.Mvc;
using DriveHub.Services; // ADD THIS

namespace DriveHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> TestEmail([FromServices] IEmailService emailService)
        {
            try
            {
                await emailService.SendEmailAsync(
                    "goodenough245@gmail.com",
                    "DriveHub SMTP Test",
                    "<h2>✅ SMTP Email Test Successful!</h2><p>This confirms your SMTP settings are working correctly.</p>"
                );
                return Content("✅ Email sent successfully! Check your inbox and spam folder.");
            }
            catch (Exception ex)
            {
                return Content($"❌ Email failed: {ex.Message}<br><br>Full error: {ex.ToString()}");
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // ADD THIS METHOD TO TEST ID NUMBERS
        public IActionResult TestId(string id = "0412075668084")
        {
            try
            {
                var (age, dob) = IdNumberHelper.CalculateAgeFromId(id);
                return Content($"✅ ID WORKS! Age: {age}, Born: {dob:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                return Content($"❌ ID FAILED: {ex.Message}");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> DebugEmail([FromServices] IEmailService emailService, [FromServices] IConfiguration config)
        {
            try
            {
                var smtpServer = config["Email:SmtpServer"];
                var smtpPort = config["Email:SmtpPort"];
                var smtpUser = config["Email:SmtpUsername"];
                var smtpPass = config["Email:SmtpPassword"];

                // Test basic SMTP connection
                using var client = new System.Net.Mail.SmtpClient(smtpServer, int.Parse(smtpPort));
                client.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;

                await client.SendMailAsync(
                    new System.Net.Mail.MailMessage(smtpUser, "goodenough245@gmail.com",
                    "SMTP Direct Test", "Testing SMTP connection directly")
                );

                return Content("✅ SMTP direct connection WORKED!");
            }
            catch (Exception ex)
            {
                return Content($"❌ SMTP failed: {ex.Message}<br><br>Full error: {ex.ToString()}");
            }
        }
    }
}