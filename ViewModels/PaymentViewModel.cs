using System.ComponentModel.DataAnnotations;

namespace DriveHub.ViewModels
{
    public class PaymentViewModel
    {
        public int PackageId { get; set; }
        public string PackageType { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        [Required]
        public string CardHolderName { get; set; } = string.Empty;

        [Required]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        public int ExpiryMonth { get; set; }

        [Required]
        public int ExpiryYear { get; set; }

        [Required]
        public string CVV { get; set; } = string.Empty;
    }
}