namespace DriveHub.Models;

public class EmailRecord
{
    public string ToEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime SentAt { get; set; }
    public string Type { get; set; } = null!; // "Receipt" or "PasswordReset"
}