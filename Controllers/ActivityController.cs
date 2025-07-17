using Microsoft.AspNetCore.Mvc;
using EducationSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EducationSystem.Models;
using EducationSystem.ViewModels;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace EducationSystem.Controllers
{
    public class ActivityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActivityController> _logger;

        public ActivityController(ApplicationDbContext context, ILogger<ActivityController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private bool AreActivitiesOverlapping(Activity newActivity, Activity existingActivity)
        {
            bool datesOverlap = newActivity.StartDate <= existingActivity.EndDate && newActivity.EndDate >= existingActivity.StartDate;
            if (!datesOverlap) return false;
            return newActivity.StartTime < newActivity.EndTime && existingActivity.StartTime < existingActivity.EndTime;
        }

        [HttpGet]
        public async Task<IActionResult> List(string filter = "all", int page = 1, string search = "", string sortBy = "newest")
        {
            try
            {
                if (page < 1) page = 1;
                const int pageSize = 9;

                ViewBag.CurrentFilter = filter;
                ViewBag.SearchQuery = search;
                ViewBag.SortBy = sortBy;

                var query = _context.Activities
                    .Include(a => a.Teacher)
                    .Include(a => a.Registrations)
                    .Include(a => a.Interactions)
                    .Where(a => a.Status == "Published"); // Only show published activities

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(a => a.Title.Contains(search) ||
                                           a.Description.Contains(search) ||
                                           (a.Skills != null && a.Skills.Contains(search)));
                }

                switch (filter)
                {
                    case "free":
                        query = query.Where(a => a.Type == "free");
                        break;
                    case "paid":
                        query = query.Where(a => a.Type == "paid");
                        break;
                    case "available":
                        query = query.Where(a => a.Registrations.Count(r => r.Status == "Approved") < a.MaxParticipants);
                        break;
                    case "registered":
                        if (User.Identity?.IsAuthenticated == true)
                        {
                            var userId = GetUserId();
                            // Filter by registrations where the current user is the student or the parent
                            query = query.Where(a => a.Registrations.Any(r => (r.StudentId == userId || r.ParentId == userId) && r.Status == "Approved"));
                        }
                        else
                        {
                            query = query.Where(a => false); // No activities if not authenticated
                        }
                        break;
                }

                query = sortBy switch
                {
                    "oldest" => query.OrderBy(a => a.CreatedAt),
                    "price_low" => query.OrderBy(a => a.Price),
                    "price_high" => query.OrderByDescending(a => a.Price),
                    "start_date" => query.OrderBy(a => a.StartDate),
                    "popular" => query.OrderByDescending(a => a.Interactions.Count(i => i.InteractionType == "Like")), // Order by likes count
                    _ => query.OrderByDescending(a => a.CreatedAt)
                };

                var totalCount = await query.CountAsync();
                var model = new ActivityListViewModel
                {
                    Activities = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(),
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    CurrentFilter = filter,
                    SearchQuery = search,
                    SortBy = sortBy
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách hoạt động với filter {Filter}", filter);
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var activity = await _context.Activities
                    .Include(a => a.Teacher)
                    .Include(a => a.Registrations)
                        .ThenInclude(r => r.Student)
                    .Include(a => a.Interactions)
                        .ThenInclude(i => i.User)
                    .FirstOrDefaultAsync(a => a.ActivityId == id);

                if (activity == null)
                {
                    _logger.LogWarning("Không tìm thấy hoạt động với ID: {ActivityId}", id);
                    return NotFound();
                }

                var userId = GetUserId();
                ViewBag.IsRegistered = User.Identity?.IsAuthenticated == true &&
                    await _context.Registrations.AnyAsync(r => (r.StudentId == userId || r.ParentId == userId) && r.ActivityId == id && r.Status != "Cancelled"); // Updated status check
                
                ViewBag.HasLiked = User.Identity?.IsAuthenticated == true &&
                    await _context.Interactions.AnyAsync(i => i.UserId == userId && i.ActivityId == id && i.InteractionType == "Like");
                
                // Cập nhật: Chỉ cho phép học sinh đăng ký
                ViewBag.CanRegister = User.Identity?.IsAuthenticated == true &&
                    !ViewBag.IsRegistered && activity.IsActive && !activity.IsFull &&
                    User.IsInRole("Student"); // <-- Đã sửa ở đây

                // Không cần lấy danh sách học sinh cho phụ huynh nữa vì chỉ học sinh mới đăng ký.
                // ViewBag.StudentsForParent = new List<ApplicationUser>();

                return View(activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải chi tiết hoạt động ID: {ActivityId}", id);
                TempData["Error"] = "Đã xảy ra lỗi khi tải chi tiết. Vui lòng thử lại.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Teachers = await _context.Teachers
                .Where(t => t.IsActive)
                .Select(t => new { t.TeacherId, t.FullName })
                .ToListAsync();
            return View(new Activity());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Activity activity)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Teachers = await _context.Teachers
                    .Where(t => t.IsActive)
                    .Select(t => new { t.TeacherId, t.FullName })
                    .ToListAsync();
                return View(activity);
            }

            try
            {
                // Check for overlapping activities (only for published activities)
                var overlappingActivities = await _context.Activities
                    .Where(a => a.ActivityId != activity.ActivityId) // Exclude self if editing
                    .Where(a => a.Status == "Published")
                    .ToListAsync();

                foreach (var existing in overlappingActivities)
                {
                    if (AreActivitiesOverlapping(activity, existing))
                    {
                        ModelState.AddModelError("", $"Hoạt động này trùng lịch với hoạt động '{existing.Title}' ({existing.StartDate.ToString("dd/MM/yyyy")} {existing.StartTime.ToString(@"hh\:mm")}).");
                        ViewBag.Teachers = await _context.Teachers
                            .Where(t => t.IsActive)
                            .Select(t => new { t.TeacherId, t.FullName })
                            .ToListAsync();
                        return View(activity);
                    }
                }

                activity.CreatedBy = GetUserId(); // Set the creator
                activity.CreatedAt = DateTime.UtcNow;
                activity.UpdatedAt = DateTime.UtcNow;
                activity.Status = "Published"; // Default status for new activities
                // CurrentParticipants is a calculated property, no need to set here

                _context.Activities.Add(activity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã tạo hoạt động {Title} bởi {CreatedBy}", activity.Title, activity.CreatedBy);
                TempData["Success"] = "Hoạt động đã được tạo thành công!";
                return RedirectToAction("Details", new { id = activity.ActivityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo hoạt động {Title}", activity.Title);
                ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo hoạt động. Vui lòng thử lại.");
                ViewBag.Teachers = await _context.Teachers
                    .Where(t => t.IsActive)
                    .Select(t => new { t.TeacherId, t.FullName })
                    .ToListAsync();
                return View(activity);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Student")] // <-- Chỉ học sinh mới có quyền đăng ký
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int activityId) // <-- Loại bỏ studentId vì học sinh tự đăng ký
        {
            try
            {
                var activity = await _context.Activities
                    .Include(a => a.Registrations)
                    .FirstOrDefaultAsync(a => a.ActivityId == activityId);

                if (activity == null || !activity.IsActive || activity.IsFull)
                {
                    _logger.LogWarning("Hoạt động ID {ActivityId} không hợp lệ hoặc đã đầy.", activityId);
                    TempData["Error"] = activity == null ? "Hoạt động không tồn tại." :
                        activity.IsFull ? "Hoạt động đã đầy." : "Hoạt động không còn mở đăng ký.";
                    return RedirectToAction("Details", new { id = activityId });
                }

                var userId = GetUserId(); // The user who is performing the registration (must be a Student)
                if (string.IsNullOrEmpty(userId))
                {
                    // This check is mostly redundant due to [Authorize(Roles = "Student")] but good for safety
                    _logger.LogWarning("Người dùng chưa đăng nhập khi đăng ký hoạt động {ActivityId}", activityId);
                    TempData["Error"] = "Vui lòng đăng nhập để đăng ký.";
                    return RedirectToAction("Login", "Account");
                }
                
                // Xác định studentId và parentId cho đăng ký
                var registrationStudentId = userId; // Học sinh luôn đăng ký cho chính mình
                var studentUser = await _context.Users.FindAsync(userId);
                var parentId = studentUser?.ParentId; // Lấy ParentId của học sinh

                // Check for existing registration for this student and activity
                var existingRegistration = await _context.Registrations
                    .FirstOrDefaultAsync(r => r.StudentId == registrationStudentId && r.ActivityId == activityId && r.Status != "Cancelled");
                if (existingRegistration != null)
                {
                    TempData["Error"] = "Đã đăng ký hoạt động này rồi.";
                    return RedirectToAction("Details", new { id = activityId });
                }

                // Check for overlapping activities for this student
                var overlappingRegistrations = await _context.Registrations
                    .Include(r => r.Activity)
                    .Where(r => r.StudentId == registrationStudentId && r.Status == "Approved")
                    .ToListAsync();
                foreach (var reg in overlappingRegistrations)
                {
                    if (reg.Activity != null && AreActivitiesOverlapping(activity, reg.Activity))
                    {
                        TempData["Error"] = $"Trùng lịch với hoạt động '{reg.Activity.Title}'.";
                        return RedirectToAction("Details", new { id = activityId });
                    }
                }

                var registration = new Registration
                {
                    UserId = userId, // Người dùng hiện tại (học sinh)
                    StudentId = registrationStudentId, // Học sinh đăng ký cho chính mình
                    ActivityId = activityId,
                    ParentId = parentId, // ParentId của học sinh (có thể null nếu học sinh không có phụ huynh)
                    RegistrationDate = DateTime.UtcNow,
                    Status = activity.Type == "free" ? "Approved" : "Pending", // Hoạt động miễn phí auto-approved, trả phí pending
                    PaymentStatus = activity.Type == "free" ? "N/A" : "Unpaid" // N/A cho miễn phí, Unpaid cho trả phí
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Học sinh {StudentId} đã đăng ký hoạt động {ActivityId} (người thực hiện {UserId})", registrationStudentId, activityId, userId);
                TempData["Success"] = "Đăng ký thành công! Vui lòng kiểm tra giỏ hàng hoặc đăng ký của tôi.";
                
                if (activity.Type == "paid")
                {
                    // Nếu là hoạt động trả phí, thêm vào giỏ hàng
                    var cartItem = new CartItem
                    {
                        UserId = userId, // Học sinh thêm vào giỏ hàng (vì họ là người đăng ký)
                        ActivityId = activityId,
                        AddedAt = DateTime.UtcNow,
                        IsPaid = false
                    };
                    _context.CartItems.Add(cartItem);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "Cart");
                }
                else
                {
                    return RedirectToAction("MyRegistrations", "Registration");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký hoạt động {ActivityId}", activityId);
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction("Details", new { id = activityId });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int activityId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thích hoạt động." });
                }

                var existingLike = await _context.Interactions
                    .FirstOrDefaultAsync(i => i.UserId == userId && i.ActivityId == activityId && i.InteractionType == "Like");
                if (existingLike != null)
                {
                    _context.Interactions.Remove(existingLike);
                    _logger.LogInformation("Người dùng {UserId} đã bỏ thích hoạt động {ActivityId}", userId, activityId);
                }
                else
                {
                    var like = new Interaction { UserId = userId, ActivityId = activityId, InteractionType = "Like", CreatedAt = DateTime.UtcNow };
                    _context.Interactions.Add(like);
                    _logger.LogInformation("Người dùng {UserId} đã thích hoạt động {ActivityId}", userId, activityId);
                }

                await _context.SaveChangesAsync();
                var likesCount = await _context.Activities.Where(a => a.ActivityId == activityId).Select(a => a.LikesCount).FirstOrDefaultAsync();
                return Json(new { success = true, likesCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thích hoạt động {ActivityId}", activityId);
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int activityId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Nội dung bình luận không được để trống." });
            }

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận." });
                }

                var comment = new Interaction
                {
                    UserId = userId,
                    ActivityId = activityId,
                    InteractionType = "Comment",
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Interactions.Add(comment);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);
                _logger.LogInformation("Người dùng {UserId} đã bình luận trên hoạt động {ActivityId}", userId, activityId);
                return Json(new
                {
                    success = true,
                    comment = new
                    {
                        content = comment.Content,
                        userName = user?.FullName ?? user?.UserName,
                        createdAt = comment.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi bình luận trên hoạt động {ActivityId}", activityId);
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }
    }
}
