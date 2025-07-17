using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Added for [ForeignKey]
using System;

namespace EducationSystem.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }
        
        [Required]
        public int RegistrationId { get; set; }
        [ForeignKey("RegistrationId")]
        public virtual Registration? Registration { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0.")]
        public decimal Amount { get; set; }
        
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        
        [StringLength(50)]
        public string? PaymentMethod { get; set; }
        
        [StringLength(255)]
        public string? TransactionId { get; set; }
        
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded
        
        [StringLength(50)]
        public string? ResponseCode { get; set; }
        
        public string? Notes { get; set; }
    }
}
