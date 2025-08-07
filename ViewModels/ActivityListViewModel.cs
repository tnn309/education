using EducationSystem.Models;
using System.Collections.Generic;

namespace EducationSystem.ViewModels
{
    public class ActivityListViewModel
    {
        public List<Activity>? Activities { get; set; } = new List<Activity>();             // Danh sách các hoạt động
        public string? CurrentFilter { get; set; } = string.Empty;      
        public string? SearchQuery { get; set; } = string.Empty;
        public string? SortBy { get; set; } = "newest";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public string? TypeText { get; set; } = string.Empty;                   // Văn bản mô tả loại hoạt động
    }
}
