using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EducationSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Display(Name = "Họ và tên")]
        [MaxLength(255)]
        public string? FullName { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Địa chỉ")]
        [MaxLength(500)]
        public string? Address { get; set; }

        [Display(Name = "Tên đăng nhập phụ huynh")]
        public string? ParentId { get; set; } // For students to link to their parent

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Calculated property for age
        public int Age
        {
            get
            {
                if (!DateOfBirth.HasValue) return 0;
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        // Navigation properties
        public virtual ICollection<ApplicationUser> Children { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Activity> CreatedActivities { get; set; } = new List<Activity>();
        public virtual ICollection<Registration> StudentRegistrations { get; set; } = new List<Registration>();
        public virtual ICollection<Registration> ParentRegistrations { get; set; } = new List<Registration>();
        public virtual ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    }
}
