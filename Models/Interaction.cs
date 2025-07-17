using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Added for [ForeignKey]
using System;

namespace EducationSystem.Models
{
    public class Interaction
    {
        [Key]
        public int InteractionId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
        
        [Required]
        public int ActivityId { get; set; }
        [ForeignKey("ActivityId")]
        public virtual Activity? Activity { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InteractionType { get; set; } = string.Empty; // Like, Comment, Share
        
        public string? Content { get; set; } // For comments
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
