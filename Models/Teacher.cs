using System.ComponentModel.DataAnnotations;

namespace EducationSystem.Models
{
    public class Teacher
    {
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Họ tên giáo viên là bắt buộc")]
        [Display(Name = "Họ và tên")]
        [MaxLength(255)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [MaxLength(255)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [MaxLength(20)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Chuyên môn")]
        [MaxLength(255)]
        public string? Specialization { get; set; }

        [Display(Name = "Kinh nghiệm (năm)")]
        [Range(0, 50, ErrorMessage = "Kinh nghiệm phải từ 0 đến 50 năm")]
        public int Experience { get; set; }

        [Display(Name = "Tiểu sử")]
        [MaxLength(2000)]
        public string? Bio { get; set; }

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
