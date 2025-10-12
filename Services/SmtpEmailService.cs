using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace DriveHub.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly bool _enableSsl;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = _configuration["Email:SmtpUsername"] ?? "your-email@gmail.com";
        _smtpPassword = _configuration["Email:SmtpPassword"] ?? "your-app-password";
        _enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        try
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                client.EnableSsl = _enableSsl;
                client.UseDefaultCredentials = false;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUsername, "DriveHub"),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }

            SentEmails++;
            Console.WriteLine($"✅ Email sent to: {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Email failed to {toEmail}: {ex.Message}");
            throw;
        }
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        var subject = "DriveHub - Password Reset Request";
        var message = $@"
            <h2>Password Reset Request</h2>
            <p>You requested to reset your password for your DriveHub account.</p>
            <p>Click the link below to reset your password:</p>
            <a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a>
            <p>If you didn't request this, please ignore this email.</p>
            <br>
            <p>Best regards,<br>DriveHub Team</p>";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string studentName)
    {
        var subject = "Welcome to DriveHub!";
        var message = $@"
            <h2>Welcome to DriveHub, {studentName}! 🚗</h2>
            <p>Thank you for registering with DriveHub Driving School. Your account has been successfully created.</p>
            <p>You can now log in to your dashboard and choose a learning package to get started with your driving lessons.</p>
            <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                <h4>Next Steps:</h4>
                <ol>
                    <li>Log in to your student dashboard</li>
                    <li>Choose a learning package that suits your needs</li>
                    <li>Complete the payment process</li>
                    <li>Start your driving lessons!</li>
                </ol>
            </div>
            <p>If you have any questions, feel free to contact us.</p>
            <br>
            <p>Happy driving!<br>DriveHub Team</p>";

        await SendEmailAsync(toEmail, subject, message);
    }

    public async Task SendReceiptEmailAsync(string toEmail, string studentName, string packageName, decimal amount, string receiptNumber)
    {
        var subject = "DriveHub - Payment Receipt";
        var message = $@"
            <h2>Payment Confirmation 🎉</h2>
            <p>Dear {studentName},</p>
            <p>Thank you for your payment! Your transaction has been processed successfully.</p>
            <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                <h4>Payment Details:</h4>
                <p><strong>Package:</strong> {packageName}</p>
                <p><strong>Amount:</strong> R{amount}</p>
                <p><strong>Receipt Number:</strong> {receiptNumber}</p>
                <p><strong>Date:</strong> {DateTime.Now.ToString("dd MMM yyyy")}</p>
            </div>
            <p>You can now access all the features of your learning package. Your instructor will be assigned to you shortly.</p>
            <p>If you have any questions about your package or lessons, please contact us.</p>
            <br>
            <p>Best regards,<br>DriveHub Team</p>";

        await SendEmailAsync(toEmail, subject, message);
    }

    public int SentEmails { get; private set; }
}