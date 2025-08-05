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
                .Include(ci => ci.User) 
                .OrderByDescending(ci => ci.AddedAt)
                .ToListAsync();

            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int activityId)
        {
            var currentUserId = GetUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                TempData["Error"] = "Vui lòng đăng nhập để thêm vào giỏ hàng.";
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Details", "Activity", new { id = activityId });
            }

            var activity = await _context.Activities.FindAsync(activityId);
            if (activity == null)
            {
                TempData["Error"] = "Hoạt động không tồn tại.";
                return RedirectToAction("Details", "Activity", new { id = activityId });
            }

            if (activity.Type == "free")
            {
                TempData["Error"] = "Hoạt động miễn phí không cần thêm vào giỏ hàng. Vui lòng đăng ký trực tiếp.";
                return RedirectToAction("Details", "Activity", new { id = activityId });
            }

            if (activity.IsFull || !activity.IsActive)
            {
                TempData["Error"] = "Hoạt động đã đầy hoặc không còn mở đăng ký.";
                return RedirectToAction("Details", "Activity", new { id = activityId });
            }
            
            var existingCartItemForCurrentUser = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == currentUserId && ci.ActivityId == activityId && !ci.IsPaid);

            if (existingCartItemForCurrentUser != null)
            {
                TempData["Error"] = "Hoạt động này đã có trong giỏ hàng của bạn.";
                return RedirectToAction("Details", "Activity", new { id = activityId });
            }

            var existingRegistration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.ActivityId == activityId && r.StudentId == currentUserId && r.Status != "Cancelled");

            if (existingRegistration != null)
            {
                TempData["Error"] = "Bạn đã đăng ký hoạt động này rồi.";
                return RedirectToAction("Details", new { id = activityId });
            }

            var cartItemForCurrentUser = new CartItem
            {
                UserId = currentUserId,
                ActivityId = activityId,
                AddedAt = DateTime.UtcNow,
                IsPaid = false
            };
            _context.CartItems.Add(cartItemForCurrentUser);

            if (await _userManager.IsInRoleAsync(currentUser, "Student") && !string.IsNullOrEmpty(currentUser.ParentId))
            {
                var parentId = currentUser.ParentId;
                var existingCartItemForParent = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.UserId == parentId && ci.ActivityId == activityId && !ci.IsPaid);

                if (existingCartItemForParent == null)
                {
                    var cartItemForParent = new CartItem
                    {
                        UserId = parentId,
                        ActivityId = activityId,
                        AddedAt = DateTime.UtcNow,
                        IsPaid = false
                    };
                    _context.CartItems.Add(cartItemForParent);
                    _logger.LogInformation("Hoạt động {ActivityId} đã được thêm vào giỏ hàng của phụ huynh {ParentId} do học sinh {StudentId} thêm.", activityId, parentId, currentUserId);
                }
            }

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
            if (currentUser == null || (await _userManager.IsInRoleAsync(currentUser, "Student") && !await _userManager.IsInRoleAsync(currentUser, "Admin")))
            {
                TempData["Error"] = "Chỉ phụ huynh hoặc quản trị viên mới có thể thanh toán.";
                return RedirectToAction("Index");
            }

            var cartItem = await _context.CartItems
                .Include(ci => ci.Activity)
                .Include(ci => ci.User) 
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
            
            try 
            {
                var existingRegistration = await _context.Registrations
                    .FirstOrDefaultAsync(r => r.ActivityId == cartItem.ActivityId && r.StudentId == cartItem.User.Id && r.Status != "Cancelled"); 

                if (existingRegistration != null && existingRegistration.PaymentStatus == "Paid")
                {
                    TempData["Error"] = "Hoạt động này đã được thanh toán và đăng ký.";
                    var relatedCartItems = await _context.CartItems
                        .Where(ci => ci.ActivityId == cartItem.ActivityId && ci.User.Id == cartItem.User.Id && !ci.IsPaid)
                        .ToListAsync();
                    _context.CartItems.RemoveRange(relatedCartItems);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }

                var payment = new Payment
                {
                    RegistrationId = existingRegistration?.RegistrationId ?? 0, 
                    UserId = userId, 
                    Amount = cartItem.Activity.Price,
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = "Online Transfer", 
                    PaymentStatus = "Completed", 
                    TransactionId = Guid.NewGuid().ToString(),
                    Notes = $"Payment for activity: {cartItem.Activity.Title} by {currentUser.FullName ?? currentUser.UserName}"
                };

                _context.Payments.Add(payment);

                if (existingRegistration != null)
                {
                    existingRegistration.Status = "Approved";
                    existingRegistration.PaymentStatus = "Paid";
                    existingRegistration.AmountPaid = payment.Amount;
                    if (existingRegistration.Activity != null && existingRegistration.Activity.CurrentParticipants < existingRegistration.Activity.MaxParticipants)
                    {
                        existingRegistration.Activity.CurrentParticipants++;
                        if (existingRegistration.Activity.CurrentParticipants >= existingRegistration.Activity.MaxParticipants)
                        {
                            existingRegistration.Activity.IsFull = true;
                            existingRegistration.Activity.Status = "Full";
                        }
                    }
                }
                else
                {
                    var studentUser = cartItem.User; 
                    var newRegistration = new Registration
                    {
                        ActivityId = cartItem.ActivityId,
                        StudentId = studentUser.Id,
                        ParentId = await _userManager.IsInRoleAsync(currentUser, "Parent") ? userId : null, 
                        UserId = studentUser.Id, 
                        RegistrationDate = DateTime.UtcNow,
                        Status = "Approved",
                        PaymentStatus = "Paid",
                        AmountPaid = payment.Amount,
                        AttendanceStatus = "Not Started"
                    };
                    _context.Registrations.Add(newRegistration);
                    if (newRegistration.Activity != null && newRegistration.Activity.CurrentParticipants < newRegistration.Activity.MaxParticipants)
                    {
                        newRegistration.Activity.CurrentParticipants++;
                        if (newRegistration.Activity.CurrentParticipants >= newRegistration.Activity.MaxParticipants)
                        {
                            newRegistration.Activity.IsFull = true;
                            newRegistration.Activity.Status = "Full";
                        }
                    }
                    payment.Registration = newRegistration; 
                }

                var studentId = cartItem.User.Id; 
                var studentUserObject = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == studentId);
                var parentOfStudent = studentUserObject?.ParentId;

                var cartItemsToRemove = await _context.CartItems
                    .Where(ci => ci.ActivityId == cartItem.ActivityId &&
                                 (ci.UserId == studentId || (parentOfStudent != null && ci.UserId == parentOfStudent)))
                    .ToListAsync();

                _context.CartItems.RemoveRange(cartItemsToRemove);

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Thanh toán thành công cho hoạt động '{cartItem.Activity.Title}'!";
                _logger.LogInformation("Người dùng {UserId} đã thanh toán cho hoạt động {ActivityId} (cho học sinh {StudentId})", userId, cartItem.ActivityId, studentId);
                return RedirectToAction("MyRegistrations", "Registration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thanh toán cho mục giỏ hàng {CartItemId}", id);
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }
    }
}
