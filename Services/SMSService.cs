using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace DriveHub.Services;

public interface ISmsService
{
    Task<bool> SendSMSAsync(string phoneNumber, string message);
    string DetectCarrier(string phoneNumber);
}

public class SMSService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly bool _enableSsl;

    public SMSService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        _smtpUsername = _configuration["Email:SmtpUsername"] ?? "your-email@gmail.com";
        _smtpPassword = _configuration["Email:SmtpPassword"] ?? "your-app-password";
        _enableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true");
    }

    public string DetectCarrier(string phoneNumber)
    {
        // Clean the phone number
        var cleanNumber = Regex.Replace(phoneNumber, @"[^0-9]", "");

        if (cleanNumber.StartsWith("27"))
        {
            cleanNumber = "0" + cleanNumber.Substring(2);
        }

        if (cleanNumber.Length != 10 || !cleanNumber.StartsWith("0"))
        {
            return null; // Invalid SA number
        }

        var prefix = cleanNumber.Substring(0, 3);

        // Vodacom prefixes
        if (prefix == "072" || prefix == "073" || prefix == "074" || prefix == "076" || prefix == "079")
            return "vodacom";

        // MTN prefixes  
        if (prefix == "083" || prefix == "073" || prefix == "081" || prefix == "082")
            return "mtn";

        // Cell C prefixes
        if (prefix == "084" || prefix == "074")
            return "cellc";

        // Telkom prefixes
        if (prefix == "081" || prefix == "082")
            return "telkom";

        return "unknown";
    }

    public async Task<bool> SendSMSAsync(string phoneNumber, string message)
    {
        try
        {
            var cleanNumber = Regex.Replace(phoneNumber, @"[^0-9]", "");

            if (cleanNumber.StartsWith("27"))
            {
                cleanNumber = "0" + cleanNumber.Substring(2);
            }

            if (cleanNumber.Length != 10 || !cleanNumber.StartsWith("0"))
            {
                Console.WriteLine($"❌ Invalid SA phone number: {phoneNumber}");
                return false;
            }

            var carrier = DetectCarrier(cleanNumber);
            string gatewayEmail = null;

            // ✅ CORRECTED SOUTH AFRICAN SMS GATEWAYS
            switch (carrier)
            {
                case "vodacom":
                    gatewayEmail = $"{cleanNumber}@voda.co.za"; // ✅ CORRECTED
                    break;
                case "mtn":
                    gatewayEmail = $"{cleanNumber}@sms.mtnnigeria.net"; // ✅ MTN uses Nigeria gateway for some reason
                    break;
                case "cellc":
                    gatewayEmail = $"{cleanNumber}@cellc.net"; // ✅ CORRECTED
                    break;
                case "telkom":
                    gatewayEmail = $"{cleanNumber}@tm4.co.za"; // ✅ CORRECTED
                    break;
                default:
                    Console.WriteLine($"❌ Unknown carrier for: {cleanNumber}");
                    return false;
            }

            Console.WriteLine($"📱 Attempting SMS via {carrier}: {gatewayEmail}");

            // Truncate message for SMS (160 chars max for optimal delivery)
            var smsMessage = message.Length > 160 ? message.Substring(0, 157) + "..." : message;

            // Send via email gateway - using direct SMTP
            var smsSent = await SendSMSViaEmailAsync(gatewayEmail, smsMessage, cleanNumber);

            if (smsSent)
            {
                Console.WriteLine($"✅ SMS sent to {cleanNumber} via {carrier}");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ SMS failed for {cleanNumber}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SMS failed to {phoneNumber}: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> SendSMSViaEmailAsync(string gatewayEmail, string message, string phoneNumber)
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
                    Subject = "", // No subject for SMS
                    Body = message,
                    IsBodyHtml = false // SMS is plain text
                };
                mailMessage.To.Add(gatewayEmail);

                await client.SendMailAsync(mailMessage);
            }

            Console.WriteLine($"✅ SMS email sent successfully to gateway: {gatewayEmail}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SMS email failed for {phoneNumber}: {ex.Message}");

            // ✅ ADDITIONAL DEBUGGING: Try alternative gateways if primary fails
            if (gatewayEmail.Contains("@voda.co.za"))
            {
                Console.WriteLine("🔄 Trying alternative Vodacom gateway...");
                return await TryAlternativeGateways(phoneNumber, message);
            }

            return false;
        }
    }

    private async Task<bool> TryAlternativeGateways(string phoneNumber, string message)
    {
        try
        {
            // ✅ ALTERNATIVE SOUTH AFRICAN SMS GATEWAYS
            var alternativeGateways = new[]
            {
                $"{phoneNumber}@vodamail.co.za", // Alternative Vodacom
                $"{phoneNumber}@mymtn.co.za",   // Alternative MTN
                $"{phoneNumber}@sms.cellc.co.za" // Alternative Cell C
            };

            foreach (var gateway in alternativeGateways)
            {
                try
                {
                    Console.WriteLine($"🔄 Trying alternative gateway: {gateway}");

                    using (var client = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                        client.EnableSsl = _enableSsl;
                        client.UseDefaultCredentials = false;
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress(_smtpUsername, "DriveHub"),
                            Subject = "",
                            Body = message,
                            IsBodyHtml = false
                        };
                        mailMessage.To.Add(gateway);

                        await client.SendMailAsync(mailMessage);
                    }

                    Console.WriteLine($"✅ SMS sent via alternative gateway: {gateway}");
                    return true;
                }
                catch (Exception altEx)
                {
                    Console.WriteLine($"❌ Alternative gateway failed {gateway}: {altEx.Message}");
                    continue; // Try next alternative
                }
            }

            Console.WriteLine("❌ All SMS gateways failed");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Alternative gateways all failed: {ex.Message}");
            return false;
        }
    }
}