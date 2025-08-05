using System.ComponentModel.DataAnnotations;

namespace EducationSystem.Models
{
    public class Message
    {
        public int MessageId { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        public int? ActivityId { get; set; } // Optional: related to specific activity

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }

        public string? MessageType { get; set; }

        // Navigation properties
        public virtual ApplicationUser Sender { get; set; } = null!;
        public virtual ApplicationUser Receiver { get; set; } = null!;
        public virtual Activity? Activity { get; set; }
    }
}
