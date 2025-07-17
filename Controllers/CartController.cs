using Microsoft.AspNetCore.Mvc;
using EducationSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EducationSystem.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace EducationSystem.Controllers
{
    [Authorize] // Giữ lại cho phép người dùng xem giỏ hàng
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger)
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
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "Vui lòng đăng nhập để xem giỏ hàng.";
                    return RedirectToAction("Login", "Account");
                }

                var cartItems = await _context.CartItems
                    .Include(c => c.Activity)
                        .ThenInclude(a => a!.Teacher) // Include Teacher for Activity
                    .Where(c => c.UserId == userId && !c.IsPaid)
                    .OrderByDescending(c => c.AddedAt)
                    .ToListAsync();

                return View(cartItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải giỏ hàng cho người dùng {UserId}", GetUserId());
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int activityId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "Vui lòng đăng nhập để thêm vào giỏ hàng.";
                    return RedirectToAction("Login", "Account");
                }

                var activity = await _context.Activities.FindAsync(activityId);
                string? errorMessage = null;

                if (activity == null)
                {
                    errorMessage = "Hoạt động không tồn tại.";
                }
                else if (activity.Type != "paid")
                {
                    errorMessage = "Hoạt động miễn phí không cần thêm vào giỏ hàng.";
                }
                else if (activity.IsFull)
                {
                    errorMessage = "Hoạt động đã đầy.";
                }
                else if (!activity.IsActive)
                {
                    errorMessage = "Hoạt động không còn mở đăng ký.";
                }

                if (errorMessage != null)
                {
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("Details", "Activity", new { id = activityId });
                }

                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ActivityId == activityId && !c.IsPaid);
                if (existingCartItem != null)
                {
                    TempData["Error"] = "Hoạt động này đã có trong giỏ hàng của bạn.";
                    return RedirectToAction("Details", "Activity", new { id = activityId });
                }

                // Check for overlapping activities in the cart for the current user
                var userCartActivities = await _context.CartItems
                    .Include(ci => ci.Activity)
                    .Where(ci => ci.UserId == userId && !ci.IsPaid)
                    .Select(ci => ci.Activity)
                    .ToListAsync();

                foreach (var cartActivity in userCartActivities)
                {
                    if (cartActivity != null && AreActivitiesOverlapping(activity!, cartActivity))
                    {
                        TempData["Error"] = $"Hoạt động '{activity!.Title}' bị trùng lịch với hoạt động '{cartActivity.Title}' đã có trong giỏ hàng.";
                        return RedirectToAction("Details", "Activity", new { id = activityId });
                    }
                }

                // Check for overlapping activities in existing registrations for the current user (as student or parent)
                var userRegisteredActivities = await _context.Registrations
                    .Include(r => r.Activity)
                    .Where(r => (r.UserId == userId || r.StudentId == userId || r.ParentId == userId) && r.Status != "Cancelled")
                    .Select(r => r.Activity)
                    .ToListAsync();

                foreach (var registeredActivity in userRegisteredActivities)
                {
                    if (registeredActivity != null && AreActivitiesOverlapping(activity!, registeredActivity))
                    {
                        TempData["Error"] = $"Hoạt động '{activity!.Title}' bị trùng lịch với hoạt động '{registeredActivity.Title}' bạn đã đăng ký.";
                        return RedirectToAction("Details", "Activity", new { id = activityId });
                    }
                }

                var cartItem = new CartItem
                {
                    UserId = userId,
                    ActivityId = activityId,
                    AddedAt = DateTime.UtcNow,
                    IsPaid = false
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Hoạt động đã được thêm vào giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm vào giỏ hàng hoạt động {ActivityId} cho người dùng {UserId}", activityId, GetUserId());
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            try
            {
                var userId = GetUserId();
                var cartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.CartItemId == id && c.UserId == userId && !c.IsPaid);

                if (cartItem == null)
                {
                    TempData["Error"] = "Không tìm thấy mục trong giỏ hàng hoặc đã được thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Mục đã được xóa khỏi giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa mục {CartItemId} khỏi giỏ hàng cho người dùng {UserId}", id, GetUserId());
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Parent")] // <-- Chỉ phụ huynh mới có quyền thanh toán
        public async Task<IActionResult> Checkout(int id)
        {
            try
            {
                var userId = GetUserId();
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Activity)
                    .FirstOrDefaultAsync(c => c.CartItemId == id && c.UserId == userId && !c.IsPaid);

                if (cartItem == null || cartItem.Activity == null)
                {
                    TempData["Error"] = "Không tìm thấy mục trong giỏ hàng hoặc hoạt động không tồn tại.";
                    return RedirectToAction("Index", "Cart");
                }

                var activity = cartItem.Activity;

                // Check if activity is still active and has available slots
                if (!activity.IsActive || activity.IsFull)
                {
                    TempData["Error"] = activity.IsFull ? "Hoạt động đã đầy." : "Hoạt động không còn mở đăng ký.";
                    _context.CartItems.Remove(cartItem); // Remove invalid item from cart
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "Cart");
                }

                // Check for overlapping activities in existing registrations for the current user (as student or parent)
                // When a parent checks out, the 'StudentId' in Registration will be the actual student (could be parent themselves or a child).
                // The overlap check should be against the *actual student's* existing registrations.
                var userRegisteredActivities = await _context.Registrations
                    .Include(r => r.Activity)
                    .Where(r => (r.StudentId == userId || r.ParentId == userId) // Check current user as student OR as parent of a registered student
                                 && r.Status != "Cancelled"
                                 && r.ActivityId != activity.ActivityId) // Exclude the activity being checked out itself
                    .Select(r => r.Activity)
                    .ToListAsync();

                foreach (var registeredActivity in userRegisteredActivities)
                {
                    if (registeredActivity != null && AreActivitiesOverlapping(activity, registeredActivity))
                    {
                        TempData["Error"] = $"Hoạt động '{activity.Title}' bị trùng lịch với hoạt động '{registeredActivity.Title}' bạn đã đăng ký.";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                // Create a new registration
                var registration = new Registration
                {
                    UserId = userId, // The parent who initiated the checkout
                    StudentId = userId, // The parent is registering themselves for this activity.
                                        // If a parent checks out for a child, the cart item should reflect the child,
                                        // or a separate flow for parent-checkout-for-child is needed.
                                        // For simplicity, assuming parent checks out for themselves.
                                        // If you need parent to checkout for a child, the CartItem model needs to store StudentId.
                    ActivityId = activity.ActivityId,
                    RegistrationDate = DateTime.UtcNow,
                    Status = "Approved", // Or "Pending" if admin approval is needed
                    PaymentStatus = "Paid", // Assuming immediate payment for checkout
                    AttendanceStatus = "Not Attended",
                    Notes = "Đăng ký qua giỏ hàng"
                };

                _context.Registrations.Add(registration);

                // Mark cart item as paid
                cartItem.IsPaid = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Người dùng {UserId} đã thanh toán thành công hoạt động {ActivityId} từ giỏ hàng.", userId, activity.ActivityId);
                TempData["Success"] = $"Bạn đã đăng ký thành công hoạt động '{activity.Title}'.";
                return RedirectToAction("MyRegistrations", "Registration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thanh toán mục {CartItemId} trong giỏ hàng cho người dùng {UserId}", id, GetUserId());
                TempData["Error"] = "Đã xảy ra lỗi trong quá trình thanh toán. Vui lòng thử lại.";
                return View("Error");
            }
        }
    }
}
