using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EducationSystem.Models
{
    public class Registration
    {
        [Key]
        public int RegistrationId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty; // The user who initiated the registration (parent or student)
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
        
        [Required]
        public string StudentId { get; set; } = string.Empty; // The actual student attending the activity
        [ForeignKey("StudentId")]
        public virtual ApplicationUser? Student { get; set; }
        
        [Required]
        public int ActivityId { get; set; }
        [ForeignKey("ActivityId")]
        public virtual Activity? Activity { get; set; }
        
        public string? ParentId { get; set; } // The parent if a student is registered by a parent
        [ForeignKey("ParentId")]
        public virtual ApplicationUser? Parent { get; set; }
        
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Paid, Refunded
        
        [StringLength(50)]
        public string AttendanceStatus { get; set; } = "Registered"; // Registered, Attended, Absent
        
        // Navigation properties
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
