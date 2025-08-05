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
    [Authorize]
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

        // GET: Activity/List
        [AllowAnonymous]
        public async Task<IActionResult> List(string searchTerm, int? teacherId, string category, int pageNumber = 1, int pageSize = 6)
        {
            IQueryable<Activity> activities = _context.Activities
                .Include(a => a.Teacher)
                .Include(a => a.Creator)
                .Include(a => a.Interactions); // Include interactions for like/comment counts

            if (!string.IsNullOrEmpty(searchTerm))
            {
                activities = activities.Where(a => a.Title.Contains(searchTerm) || a.Description.Contains(searchTerm));
            }

            if (teacherId.HasValue)
            {
                activities = activities.Where(a => a.TeacherId == teacherId.Value);
            }

            if (!string.IsNullOrEmpty(category))
            {
                activities = activities.Where(a => a.Category == category);
            }

            // Get current user ID for like status
            var currentUserId = _userManager.GetUserId(User);

            var totalCount = await activities.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedActivities = await activities
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var activity in pagedActivities)
            {
                activity.CurrentRegistrationsCount = await _context.Registrations
                    .CountAsync(r => r.ActivityId == activity.Id && r.Status == "Confirmed");
                activity.LikesCount = activity.Interactions.Count(i => i.Type == "Like");
                activity.CommentsCount = activity.Interactions.Count(i => i.Type == "Comment");
                activity.IsLikedByUser = !string.IsNullOrEmpty(currentUserId) && activity.Interactions.Any(i => i.Type == "Like" && i.UserId == currentUserId);
            }

            var viewModel = new ActivityListViewModel
            {
                Activities = pagedActivities,
                PageNumber = pageNumber,
                TotalPages = totalPages,
                SearchTerm = searchTerm,
                TeacherId = teacherId,
                Category = category
            };

            ViewBag.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
            ViewBag.Categories = await _context.Activities.Select(a => a.Category).Distinct().ToListAsync();

            return View(viewModel);
        }

        // GET: Activity/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.Activities
                .Include(a => a.Teacher)
                .Include(a => a.Creator)
                .Include(a => a.Interactions)
                    .ThenInclude(i => i.User) // Include user for comments
                .FirstOrDefaultAsync(m => m.Id == id);

            if (activity == null)
            {
                return NotFound();
            }

            activity.CurrentRegistrationsCount = await _context.Registrations
                .CountAsync(r => r.ActivityId == activity.Id && r.Status == "Confirmed");
            activity.LikesCount = activity.Interactions.Count(i => i.Type == "Like");
            activity.CommentsCount = activity.Interactions.Count(i => i.Type == "Comment");

            var currentUserId = _userManager.GetUserId(User);
            activity.IsLikedByUser = !string.IsNullOrEmpty(currentUserId) && activity.Interactions.Any(i => i.Type == "Like" && i.UserId == currentUserId);

            ViewBag.Comments = activity.Interactions.Where(i => i.Type == "Comment").OrderByDescending(i => i.CreatedAt).ToList();

            return View(activity);
        }

        // POST: Activity/ToggleLike
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int activityId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này." });
            }

            var existingLike = await _context.Interactions
                .FirstOrDefaultAsync(i => i.ActivityId == activityId && i.UserId == userId && i.Type == "Like");

            if (existingLike == null)
            {
                // Add like
                var like = new Interaction
                {
                    ActivityId = activityId,
                    UserId = userId,
                    Type = "Like",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Interactions.Add(like);
            }
            else
            {
                // Remove like
                _context.Interactions.Remove(existingLike);
            }

            await _context.SaveChangesAsync();

            var likesCount = await _context.Interactions.CountAsync(i => i.ActivityId == activityId && i.Type == "Like");
            var isLiked = existingLike == null; // If it was null, now it's liked

            return Json(new { success = true, likesCount = likesCount, isLiked = isLiked });
        }

        // POST: Activity/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddComment(int activityId, string commentContent)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để bình luận." });
            }

            if (string.IsNullOrWhiteSpace(commentContent))
            {
                return Json(new { success = false, message = "Nội dung bình luận không được để trống." });
            }

            var comment = new Interaction
            {
                ActivityId = activityId,
                UserId = userId,
                Type = "Comment",
                Content = commentContent,
                CreatedAt = DateTime.UtcNow
            };

            _context.Interactions.Add(comment);
            await _context.SaveChangesAsync();

            var commentsCount = await _context.Interactions.CountAsync(i => i.ActivityId == activityId && i.Type == "Comment");
            var user = await _userManager.FindByIdAsync(userId);

            return Json(new
            {
                success = true,
                commentsCount = commentsCount,
                comment = new
                {
                    userName = user.FullName ?? user.UserName,
                    content = comment.Content,
                    createdAt = comment.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                }
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Create([Bind("Title,Description,StartTime,EndTime,Price,Capacity,Location,ImageUrl,Category,MinAge,MaxAge,Skills,Requirements,TeacherId")] Activity activity)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                activity.CreatorId = userId;
                activity.CreatedAt = DateTime.UtcNow;

                _context.Add(activity);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Hoạt động đã được tạo thành công!";
                return RedirectToAction(nameof(List));
            }
            ViewBag.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
            return View(activity);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.Activities.FindAsync(id);
            if (activity == null)
            {
                return NotFound();
            }

            // Only allow Admin or the creator of the activity to edit
            var currentUserId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && activity.CreatorId != currentUserId)
            {
                return Forbid(); // Or Redirect to Access Denied
            }

            ViewBag.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
            return View(activity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,StartTime,EndTime,Price,Capacity,Location,ImageUrl,Category,MinAge,MaxAge,Skills,Requirements,TeacherId,CreatorId,CreatedAt")] Activity activity)
        {
            if (id != activity.Id)
            {
                return NotFound();
            }

            // Only allow Admin or the creator of the activity to edit
            var existingActivity = await _context.Activities.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (existingActivity == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && existingActivity.CreatorId != currentUserId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    activity.UpdatedAt = DateTime.UtcNow;
                    // Preserve CreatorId and CreatedAt from existing activity
                    activity.CreatorId = existingActivity.CreatorId;
                    activity.CreatedAt = existingActivity.CreatedAt;

                    _context.Update(activity);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Hoạt động đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActivityExists(activity.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(List));
            }
            ViewBag.Teachers = await _context.Teachers.OrderBy(t => t.FullName).ToListAsync();
            return View(activity);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.Activities
                .Include(a => a.Teacher)
                .Include(a => a.Creator)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (activity == null)
            {
                return NotFound();
            }

            // Only allow Admin or the creator of the activity to delete
            var currentUserId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && activity.CreatorId != currentUserId)
            {
                return Forbid();
            }

            return View(activity);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null)
            {
                return NotFound();
            }

            // Only allow Admin or the creator of the activity to delete
            var currentUserId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && activity.CreatorId != currentUserId)
            {
                return Forbid();
            }

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Hoạt động đã được xóa thành công!";
            return RedirectToAction(nameof(List));
        }

        private bool ActivityExists(int id)
        {
            return _context.Activities.Any(e => e.Id == id);
        }
    }
}
