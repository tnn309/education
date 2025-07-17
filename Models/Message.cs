using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Added for [ForeignKey]
using System;
using System.Collections.Generic;

namespace EducationSystem.Models
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }
        
        [Required]
        public string SenderId { get; set; } = string.Empty;
        [ForeignKey("SenderId")]
        public virtual ApplicationUser? Sender { get; set; }
        
        public string? RecipientId { get; set; }
        [ForeignKey("RecipientId")]
        public virtual ApplicationUser? Recipient { get; set; }
        
        [StringLength(255)]
        public string? Subject { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public bool IsRead { get; set; } = false;
        
        [StringLength(50)]
        public string MessageType { get; set; } = "General"; // General, Support, Registration, Payment
        
        public int? RelatedActivityId { get; set; }
        [ForeignKey("RelatedActivityId")]
        public virtual Activity? RelatedActivity { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
