using Microsoft.AspNetCore.Mvc;
using EducationSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EducationSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace EducationSystem.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<CartController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.CartItems
                .Where(ci => ci.UserId == userId && !ci.IsPaid)
                .Include(ci => ci.Activity)
                .Include(ci => ci.User) // Include the user who added the item to cart
                .OrderByDescending(ci => ci.AddedAt)
                .ToListAsync();

            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int activityId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để thêm vào giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            var activity = await _context.Activities.FindAsync(activityId);
            if (activity == null || activity.Type == "free")
            {
                TempData["Error"] = "Hoạt động không tồn tại hoặc là hoạt động miễn phí.";
                return RedirectToAction("Details", "Activity", new { id = activityId });
            }

            if (activity.IsFull || !activity.IsActive)
            {
                TempData["Error"] = "Hoạt động đã đầy hoặc không còn mở đăng ký.";
                return RedirectToAction("Details", "Activity", new { id = activityId });
            }

            // Check if already in cart
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ActivityId == activityId && !ci.IsPaid);

            if (existingCartItem != null)
            {
                TempData["Error"] = "Hoạt động này đã có trong giỏ hàng của bạn.";
                return RedirectToAction("Details", "Activity", new { id = activityId });
            }

            // Check if already registered (even if not paid yet)
            var existingRegistration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.StudentId == userId && r.ActivityId == activityId && r.Status != "Cancelled");

            if (existingRegistration != null)
            {
                TempData["Error"] = "Bạn đã đăng ký hoạt động này rồi.";
                return RedirectToAction("Details", "Activity", new { id = activityId });
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
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.CartItemId == id && ci.UserId == userId);

            if (cartItem == null)
            {
                TempData["Error"] = "Không tìm thấy mục trong giỏ hàng.";
                return RedirectToAction("Index");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mục đã được xóa khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để thanh toán.";
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null || await _userManager.IsInRoleAsync(currentUser, "Student"))
            {
                TempData["Error"] = "Chỉ phụ huynh hoặc quản trị viên mới có thể thanh toán.";
                return RedirectToAction("Index");
            }

            var cartItem = await _context.CartItems
                .Include(ci => ci.Activity)
                .Include(ci => ci.User) // The user who added to cart (student)
                .FirstOrDefaultAsync(ci => ci.CartItemId == id && ci.UserId == userId && !ci.IsPaid);

            if (cartItem == null || cartItem.Activity == null || cartItem.User == null)
            {
                TempData["Error"] = "Mục giỏ hàng không hợp lệ hoặc không tìm thấy.";
                return RedirectToAction("Index");
            }

            if (cartItem.Activity.Type == "free")
            {
                TempData["Error"] = "Hoạt động miễn phí không cần thanh toán.";
                return RedirectToAction("Index");
            }

            // Check if a registration already exists for this activity and student
            var existingRegistration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.ActivityId == cartItem.ActivityId && r.StudentId == cartItem.User.Id && r.Status != "Cancelled");

            if (existingRegistration != null && existingRegistration.PaymentStatus == "Paid")
            {
                TempData["Error"] = "Hoạt động này đã được thanh toán và đăng ký.";
                _context.CartItems.Remove(cartItem); // Remove from cart if already paid
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            // Simulate payment process
            var payment = new Payment
            {
                RegistrationId = existingRegistration?.RegistrationId ?? 0, // Link to existing registration if any
                UserId = userId, // The user making the payment (parent/admin)
                Amount = cartItem.Activity.Price,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "Online Transfer", // Example method
                PaymentStatus = "Completed", // Simulate successful payment
                TransactionId = Guid.NewGuid().ToString(),
                Notes = $"Payment for activity: {cartItem.Activity.Title} by {currentUser.FullName ?? currentUser.UserName}"
            };

            _context.Payments.Add(payment);

            // Update registration status if it exists, or create a new one
            if (existingRegistration != null)
            {
                existingRegistration.Status = "Approved";
                existingRegistration.PaymentStatus = "Paid";
                existingRegistration.AmountPaid = payment.Amount;
                existingRegistration.Activity.CurrentParticipants++; // Increment participant count
                if (existingRegistration.Activity.CurrentParticipants >= existingRegistration.Activity.MaxParticipants)
                {
                    existingRegistration.Activity.IsFull = true;
                    existingRegistration.Activity.Status = "Full";
                }
            }
            else
            {
                // Create a new registration if it doesn't exist (e.g., added to cart directly by parent)
                var studentUser = cartItem.User; // The student associated with this cart item
                var newRegistration = new Registration
                {
                    ActivityId = cartItem.ActivityId,
                    StudentId = studentUser.Id,
                    ParentId = userId, // The parent making the payment
                    UserId = studentUser.Id, // For compatibility
                    RegistrationDate = DateTime.UtcNow,
                    Status = "Approved",
                    PaymentStatus = "Paid",
                    AmountPaid = payment.Amount,
                    AttendanceStatus = "Not Started"
                };
                _context.Registrations.Add(newRegistration);
                newRegistration.Activity.CurrentParticipants++; // Increment participant count
                if (newRegistration.Activity.CurrentParticipants >= newRegistration.Activity.MaxParticipants)
                {
                    newRegistration.Activity.IsFull = true;
                    newRegistration.Activity.Status = "Full";
                }
                payment.Registration = newRegistration; // Link payment to new registration
            }

            cartItem.IsPaid = true; // Mark cart item as paid
            _context.CartItems.Remove(cartItem); // Remove from cart after successful payment

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Thanh toán thành công cho hoạt động '{cartItem.Activity.Title}'!";
            _logger.LogInformation("Người dùng {UserId} đã thanh toán cho hoạt động {ActivityId}", userId, cartItem.ActivityId);
            return RedirectToAction("MyRegistrations", "Registration");
        }
    }
}
