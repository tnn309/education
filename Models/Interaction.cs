using System.ComponentModel.DataAnnotations;

namespace EducationSystem.Models
{
    public class Interaction
    {
        public int InteractionId { get; set; }

        [Required]
        public int ActivityId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string InteractionType { get; set; } = string.Empty; // "Like" or "Comment"

        public string? Content { get; set; } // For comments

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual Activity Activity { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
