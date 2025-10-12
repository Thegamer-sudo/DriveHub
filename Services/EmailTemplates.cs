namespace DriveHub.Services
{
    public static class EmailTemplates
    {
        public static string GetPaymentConfirmationEmail(string studentName, string packageName, decimal amount, string receiptNumber)
        {
            return $@"
Payment Confirmed - Receipt #{receiptNumber}

Dear {studentName},

Thank you for your payment! Your {packageName} has been activated successfully.

📋 Payment Details:
• Package: {packageName}
• Amount: R{amount}
• Receipt #: {receiptNumber}
• Date: {DateTime.Now:dd MMM yyyy}

Your lessons are now ready to be booked. Log in to your dashboard to schedule your first driving lesson!

Happy Learning,
DriveHub Team 🚗
";
        }

        public static string GetPasswordResetEmail(string resetLink)
        {
            return $@"
Password Reset Request

We received a request to reset your DriveHub password.

Click the link below to reset your password:
{resetLink}

If you didn't request this, please ignore this email.

The link will expire in 1 hour.

Safe driving,
DriveHub Team 🚗
";
        }
    }
}