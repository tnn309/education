using System.ComponentModel.DataAnnotations;

namespace EducationSystem.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty; // Who added to cart

        [Required]
        public int ActivityId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public bool IsPaid { get; set; } = false;

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Activity Activity { get; set; } = null!;
    }
}
