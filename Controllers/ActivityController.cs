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
using Microsoft.AspNetCore.Identity;

namespace EducationSystem.Controllers
{
    public class ActivityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActivityController> _logger;
        private readonly UserManager<ApplicationUser> _userManager; 

        public ActivityController(ApplicationDbContext context, ILogger<ActivityController> logger, UserManager<ApplicationUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private bool AreActivitiesOverlapping(Activity newActivity, Activity existingActivity)
        {
            bool datesOverlap = newActivity.StartDate <= existingActivity.EndDate && newActivity.EndDate >= existingActivity.StartDate;
            if (!datesOverlap) return false;
            
            // Check time overlap only if dates overlap
            return (newActivity.StartTime < existingActivity.EndTime && newActivity.EndTime > existingActivity.StartTime);
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
                            query = query.Where(a => a.Registrations.Any(r => (r.StudentId == userId || r.ParentId == userId) && r.Status == "Approved"));
                        }
                        else
                        {
                            query = query.Where(a => false);
                        }
                        break;
                }

                query = sortBy switch
                {
                    "oldest" => query.OrderBy(a => a.CreatedAt),
                    "price_low" => query.OrderBy(a => a.Price),
                    "price_high" => query.OrderByDescending(a => a.Price),
                    "start_date" => query.OrderBy(a => a.StartDate),
                    "popular" => query.OrderByDescending(a => a.Interactions.Count(i => i.InteractionType == "Like")),
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
                    .Include(a => a.Creator)
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
                    await _context.Registrations.AnyAsync(r => (r.StudentId == userId || r.ParentId == userId) && r.ActivityId == id && r.Status != "Cancelled");

                ViewBag.HasLiked = User.Identity?.IsAuthenticated == true &&
                    await _context.Interactions.AnyAsync(i => i.UserId == userId && i.ActivityId == id && i.InteractionType == "Like");

                // Only allow students to register for free activities directly
                ViewBag.CanRegisterFree = User.Identity?.IsAuthenticated == true &&
                    !ViewBag.IsRegistered && activity.IsActive && !activity.IsFull &&
                    User.IsInRole("Student") && activity.Type == "free";

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
            activity.StartDate = DateTime.SpecifyKind(activity.StartDate, DateTimeKind.Utc);
            activity.EndDate = DateTime.SpecifyKind(activity.EndDate, DateTimeKind.Utc);

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
                var overlappingActivities = await _context.Activities
                    .Where(a => a.ActivityId != activity.ActivityId)
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

                var userId = GetUserId();
                activity.CreatedBy = userId;
                activity.CreatorId = userId;
                activity.CreatedAt = DateTime.UtcNow; 
                activity.UpdatedAt = DateTime.UtcNow; 
                activity.Status = "Published";
                activity.LikesCount = 0;
                activity.CommentsCount = 0;
                activity.CurrentParticipants = 0;
                activity.IsActive = true;
                activity.IsFull = false;

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
        [Authorize(Roles = "Student")] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterFree(int activityId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "Vui lòng đăng nhập để đăng ký.";
                    return RedirectToAction("Login", "Account");
                }

                var user = await _userManager.FindByIdAsync(userId);
                var activity = await _context.Activities.FindAsync(activityId);

                if (activity == null)
                {
                    TempData["Error"] = "Hoạt động không tồn tại.";
                    return RedirectToAction("List");
                }

                if (activity.Type != "free")
                {
                    TempData["Error"] = "Hoạt động này không miễn phí.";
                    return RedirectToAction("Details", new { id = activityId });
                }

                if (!activity.IsActive)
                {
                    TempData["Error"] = "Hoạt động đã đóng đăng ký.";
                    return RedirectToAction("Details", new { id = activityId });
                }

                if (activity.IsFull)
                {
                    TempData["Error"] = "Hoạt động đã đầy.";
                    return RedirectToAction("Details", new { id = activityId });
                }

                var existingRegistration = await _context.Registrations
                    .FirstOrDefaultAsync(r => r.ActivityId == activityId && r.StudentId == userId && r.Status != "Cancelled");

                if (existingRegistration != null)
                {
                    TempData["Error"] = "Bạn đã đăng ký hoạt động này rồi.";
                    return RedirectToAction("Details", new { id = activityId });
                }

                if (user?.DateOfBirth.HasValue == true)
                {
                    var age = DateTime.UtcNow.Year - user.DateOfBirth.Value.Year;
                    if (user.DateOfBirth.Value.Date > DateTime.UtcNow.AddYears(-age)) age--;

                    if (age < activity.MinAge || age > activity.MaxAge)
                    {
                        TempData["Error"] = $"Độ tuổi của bạn không phù hợp với yêu cầu ({activity.MinAge}-{activity.MaxAge} tuổi).";
                        return RedirectToAction("Details", new { id = activityId });
                    }
                }

                var overlappingRegistrations = await _context.Registrations
                    .Include(r => r.Activity)
                    .Where(r => r.StudentId == userId && r.Status == "Approved")
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
                    ActivityId = activityId,
                    StudentId = userId,
                    UserId = userId, 
                    ParentId = !string.IsNullOrEmpty(user?.ParentId) ? user.ParentId : null,
                    RegistrationDate = DateTime.UtcNow, 
                    Status = "Approved", 
                    PaymentStatus = "N/A", 
                    Notes = "Đăng ký hoạt động miễn phí",
                    AmountPaid = 0,
                    AttendanceStatus = "Not Started"
                };

                _context.Registrations.Add(registration);

                activity.CurrentParticipants++;
                if (activity.CurrentParticipants >= activity.MaxParticipants)
                {
                    activity.IsFull = true;
                    activity.Status = "Full"; 
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Đăng ký hoạt động thành công!";
                _logger.LogInformation("Học sinh {StudentId} đã đăng ký hoạt động miễn phí {ActivityId}", userId, activityId);
                return RedirectToAction("MyRegistrations", "Registration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký hoạt động miễn phí {ActivityId}", activityId);
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

                var activity = await _context.Activities.FindAsync(activityId);
                if (activity == null)
                {
                    return Json(new { success = false, message = "Hoạt động không tồn tại." });
                }

                var existingLike = await _context.Interactions
                    .FirstOrDefaultAsync(i => i.UserId == userId && i.ActivityId == activityId && i.InteractionType == "Like");

                bool hasLiked;
                if (existingLike != null)
                {
                    _context.Interactions.Remove(existingLike);
                    activity.LikesCount = Math.Max(0, activity.LikesCount - 1);
                    hasLiked = false;
                    _logger.LogInformation("Người dùng {UserId} đã bỏ thích hoạt động {ActivityId}", userId, activityId);
                }
                else
                {
                    var like = new Interaction 
                    { 
                        UserId = userId, 
                        ActivityId = activityId, 
                        InteractionType = "Like", 
                        CreatedAt = DateTime.UtcNow 
                    };
                    _context.Interactions.Add(like);
                    activity.LikesCount++;
                    hasLiked = true;
                    _logger.LogInformation("Người dùng {UserId} đã thích hoạt động {ActivityId}", userId, activityId);
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    likesCount = activity.LikesCount,
                    hasLiked = hasLiked
                });
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

                var user = await _userManager.FindByIdAsync(userId);
                var activity = await _context.Activities.FindAsync(activityId);

                if (activity == null)
                {
                    return Json(new { success = false, message = "Hoạt động không tồn tại." });
                }

                var comment = new Interaction
                {
                    UserId = userId,
                    ActivityId = activityId,
                    InteractionType = "Comment",
                    Content = content.Trim(),
                    CreatedAt = DateTime.UtcNow 
                };

                _context.Interactions.Add(comment);
                activity.CommentsCount++;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Người dùng {UserId} đã bình luận trên hoạt động {ActivityId}", userId, activityId);

                return Json(new
                {
                    success = true,
                    commentsCount = activity.CommentsCount,
                    comment = new
                    {
                        content = comment.Content,
                        userName = user?.FullName ?? user?.UserName ?? "Người dùng",
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
