using System.ComponentModel.DataAnnotations;

namespace EducationSystem.Models
{
    public class Activity
    {
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại hoạt động là bắt buộc")]
        [Display(Name = "Loại")]
        public string Type { get; set; } = string.Empty; // "free" or "paid"

        [Display(Name = "Giá")]
        public decimal Price { get; set; }

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Số lượng tối đa là bắt buộc")]
        [Display(Name = "Số lượng tối đa")]
        [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 đến 1000")]
        public int MaxParticipants { get; set; }

        [Display(Name = "Số người đã đăng ký")]
        public int CurrentParticipants { get; set; }

        [Required(ErrorMessage = "Tuổi tối thiểu là bắt buộc")]
        [Display(Name = "Tuổi tối thiểu")]
        [Range(3, 18, ErrorMessage = "Tuổi phải từ 3 đến 18")]
        public int MinAge { get; set; }

        [Required(ErrorMessage = "Tuổi tối đa là bắt buộc")]
        [Display(Name = "Tuổi tối đa")]
        [Range(3, 18, ErrorMessage = "Tuổi phải từ 3 đến 18")]
        public int MaxAge { get; set; }

        [Required(ErrorMessage = "Địa điểm là bắt buộc")]
        [Display(Name = "Địa điểm")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Giờ bắt đầu là bắt buộc")]
        [Display(Name = "Giờ bắt đầu")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Giờ kết thúc là bắt buộc")]
        [Display(Name = "Giờ kết thúc")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Kỹ năng")]
        public string? Skills { get; set; }

        [Display(Name = "Yêu cầu")]
        public string? Requirements { get; set; }

        [Display(Name = "Giáo viên")]
        public int? TeacherId { get; set; }

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; }

        [Display(Name = "Đã đầy")]
        public bool IsFull { get; set; }

        [Display(Name = "Số lượt thích")]
        public int LikesCount { get; set; }

        [Display(Name = "Số bình luận")]
        public int CommentsCount { get; set; }

        // Additional properties for compatibility
        [Display(Name = "Trạng thái xuất bản")]
        // Updated to include "Full" as a possible status for capacity management
        public string Status { get; set; } = "Published"; // "Draft", "Published", "Archived", "Full"

        [Display(Name = "Người tạo")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Người tạo (ID)")]
        public string? CreatorId { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public virtual Teacher? Teacher { get; set; }
        public virtual ApplicationUser? Creator { get; set; }
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
        public virtual ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
