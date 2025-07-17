using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace EducationSystem.Models
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Tiêu đề hoạt động là bắt buộc.")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả hoạt động là bắt buộc.")]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Loại hoạt động là bắt buộc (miễn phí/trả phí).")]
        [StringLength(50)]
        public string Type { get; set; } = "free"; // "free" or "paid"

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải là số không âm.")]
        public decimal Price { get; set; } = 0;

        [Required(ErrorMessage = "Số lượng người tham gia tối đa là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng người tham gia tối đa phải lớn hơn 0.")]
        public int MaxParticipants { get; set; } = 20;

        [Required(ErrorMessage = "Độ tuổi tối thiểu là bắt buộc.")]
        [Range(0, 100, ErrorMessage = "Độ tuổi tối thiểu phải từ 0 đến 100.")]
        public int MinAge { get; set; } = 6;

        [Required(ErrorMessage = "Độ tuổi tối đa là bắt buộc.")]
        [Range(0, 100, ErrorMessage = "Độ tuổi tối đa phải từ 0 đến 100.")]
        public int MaxAge { get; set; } = 18;

        [StringLength(500)]
        public string? Skills { get; set; } // Kỹ năng cần thiết hoặc đạt được

        [StringLength(1000)]
        public string? Requirements { get; set; } // Yêu cầu đặc biệt

        [Required(ErrorMessage = "Địa điểm là bắt buộc.")]
        [StringLength(255)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc.")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc.")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc.")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true; // Hoạt động có đang mở đăng ký không

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Published"; // e.g., Draft, Published, Archived, Full, Cancelled, Completed

        // Foreign key for Teacher
        public int? TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public Teacher? Teacher { get; set; }

        // Foreign key for Creator (ApplicationUser)
        public string? CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public ApplicationUser? Creator { get; set; }

        // Navigation properties
        public ICollection<Registration>? Registrations { get; set; } = new List<Registration>();
        public ICollection<CartItem>? CartItems { get; set; } = new List<CartItem>();
        public ICollection<Interaction>? Interactions { get; set; } = new List<Interaction>(); // Likes, Comments
        public ICollection<Message>? Messages { get; set; } = new List<Message>(); // Messages related to this activity

        // Helper properties (NotMapped means they are not stored in the database)
        [NotMapped]
        public int CurrentParticipants => Registrations?.Count(r => r.Status == "Approved") ?? 0;
        [NotMapped]
        public int AvailableSlots => MaxParticipants - CurrentParticipants;
        [NotMapped]
        public bool IsFull => AvailableSlots <= 0;
        [NotMapped]
        public int LikesCount => Interactions?.Count(i => i.InteractionType == "Like") ?? 0;
        [NotMapped]
        public int CommentsCount => Interactions?.Count(i => i.InteractionType == "Comment") ?? 0;
    }
}
