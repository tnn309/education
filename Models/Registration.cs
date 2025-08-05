using System.ComponentModel.DataAnnotations;

namespace EducationSystem.Models
{
    public class Registration
    {
        public int RegistrationId { get; set; }

        [Required]
        public int ActivityId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        public string? ParentId { get; set; }

        // Additional property for compatibility
        public string? UserId { get; set; }

        public DateTime RegistrationDate { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty; // "Pending", "Approved", "Rejected", "Cancelled"

        [Required]
        public string PaymentStatus { get; set; } = string.Empty; // "Paid", "Unpaid", "Refunded", "N/A"

        public string? Notes { get; set; }

        // Additional properties for compatibility
        public string? AttendanceStatus { get; set; } = "Not Started"; // "Not Started", "Present", "Absent", "Completed"
        
        public decimal? AmountPaid { get; set; }

        // Navigation properties
        public virtual Activity Activity { get; set; } = null!;
        public virtual ApplicationUser Student { get; set; } = null!;
        public virtual ApplicationUser? Parent { get; set; }
        public virtual ApplicationUser? User { get; set; } // For compatibility
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
