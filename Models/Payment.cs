using System.ComponentModel.DataAnnotations;

namespace EducationSystem.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        [Required]
        public int RegistrationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty; // Who made the payment

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty; // "Cash", "Card", "Transfer"

        [Required]
        public string PaymentStatus { get; set; } = string.Empty; // "Pending", "Completed", "Failed", "Refunded"

        public string? TransactionId { get; set; }

        public string? ResponseCode { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        public virtual Registration Registration { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
