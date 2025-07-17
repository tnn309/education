using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Added for [ForeignKey]
using System;

namespace EducationSystem.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }
        
        [Required] // UserId is required for a cart item
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
        
        [Required]
        public int ActivityId { get; set; }
        [ForeignKey("ActivityId")]
        public virtual Activity? Activity { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsPaid { get; set; } = false;
    }
}
