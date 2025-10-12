namespace DriveHub.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string message);
    Task SendPasswordResetAsync(string toEmail, string resetLink);
    Task SendWelcomeEmailAsync(string toEmail, string studentName);
    Task SendReceiptEmailAsync(string toEmail, string studentName, string packageName, decimal amount, string receiptNumber);
    int SentEmails { get; }
}