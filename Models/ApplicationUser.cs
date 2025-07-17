using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EducationSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FullName { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        public string? Address { get; set; }
        
        // Liên kết học sinh với phụ huynh
        public string? ParentId { get; set; }
        public virtual ApplicationUser? Parent { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<ApplicationUser> Children { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Activity> CreatedActivities { get; set; } = new List<Activity>();
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>(); // Added for UserId in Registration
        public virtual ICollection<Registration> StudentRegistrations { get; set; } = new List<Registration>();
        public virtual ICollection<Registration> ParentRegistrations { get; set; } = new List<Registration>();
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        
        // Helper properties
        public int Age => DateOfBirth.HasValue ? 
            DateTime.Today.Year - DateOfBirth.Value.Year - 
            (DateTime.Today.DayOfYear < DateOfBirth.Value.DayOfYear ? 1 : 0) : 0;
            
        public bool IsStudent => !string.IsNullOrEmpty(ParentId);
        public bool IsParent => Children.Any();
    }
}
