using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DriveHub.Models;

public class Receipt
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public int PackageId { get; set; }
    public Package Package { get; set; } = null!;
    public string CardHolderName { get; set; } = null!;
    public string Last4Digits { get; set; } = null!;
    public DateTime Expiry { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string ReceiptNumber { get; set; } = null!;
}