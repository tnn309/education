using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace EducationSystem.Models
{
    public class Teacher
    {
        [Key]
        public int TeacherId { get; set; }
        
        [Required(ErrorMessage = "Họ tên giáo viên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;
        
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(256)]
        public string? Email { get; set; }
        
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(100)]
        public string? Specialization { get; set; } // Chuyên môn
        
        [Range(0, 50, ErrorMessage = "Kinh nghiệm phải từ 0 đến 50 năm.")]
        public int Experience { get; set; } = 0; // Số năm kinh nghiệm
        
        public string? Bio { get; set; } // Tiểu sử
        
        public bool IsActive { get; set; } = true; // Trạng thái hoạt động
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
