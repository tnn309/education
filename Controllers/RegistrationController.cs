using Microsoft.AspNetCore.Mvc;
using EducationSystem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EducationSystem.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq; // Added for .Any()

namespace EducationSystem.Controllers
{
    [Authorize]
    public class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistrationController> _logger;

        public RegistrationController(ApplicationDbContext context, ILogger<RegistrationController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> MyRegistrations()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "Vui lòng đăng nhập để xem đăng ký.";
                    return RedirectToAction("Login", "Account");
                }

                var registrations = await _context.Registrations
                    .Include(r => r.Activity)
                    .Include(r => r.Student)
                    .Include(r => r.Parent)
                    // Filter registrations where current user is the one who initiated it (UserId),
                    // or is the student (StudentId), or is the parent (ParentId)
                    .Where(r => r.UserId == userId || r.StudentId == userId || r.ParentId == userId)
                    .OrderByDescending(r => r.RegistrationDate)
                    .ToListAsync();

                return View(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách đăng ký cho người dùng {UserId}", GetUserId());
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Parent,Student,Admin")]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            try
            {
                var userId = GetUserId();
                var registration = await _context.Registrations
                    .Include(r => r.Activity)
                    .FirstOrDefaultAsync(r => r.RegistrationId == id);

                if (registration == null)
                {
                    TempData["Error"] = "Đăng ký không tồn tại.";
                    return NotFound();
                }

                // Check if the current user is the one who registered (UserId), or the student (StudentId), or the parent (ParentId), or an Admin
                if (registration.UserId != userId && registration.StudentId != userId && registration.ParentId != userId && !User.IsInRole("Admin"))
                {
                    TempData["Error"] = "Bạn không có quyền hủy đăng ký này.";
                    return Forbid();
                }

                if (registration.Activity != null && registration.Activity.StartDate <= DateTime.UtcNow.Date)
                {
                    TempData["Error"] = "Không thể hủy đăng ký cho hoạt động đã bắt đầu.";
                    return RedirectToAction(nameof(MyRegistrations));
                }

                registration.Status = "Cancelled";
                registration.Notes = "Hủy bởi " + (User.IsInRole("Admin") ? "quản trị viên" : "người dùng");
                if (registration.PaymentStatus == "Paid")
                {
                    registration.PaymentStatus = "Refund Pending";
                }

                _context.Registrations.Update(registration);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đăng ký {RegistrationId} đã được hủy bởi {UserId}", id, userId);
                TempData["Success"] = "Đăng ký đã được hủy thành công.";
                return RedirectToAction(nameof(MyRegistrations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hủy đăng ký {RegistrationId}", id);
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction(nameof(MyRegistrations));
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageRegistrations()
        {
            try
            {
                var registrations = await _context.Registrations
                    .Include(r => r.Student)
                    .Include(r => r.Parent)
                    .Include(r => r.Activity)
                    .OrderByDescending(r => r.RegistrationDate)
                    .ToListAsync();
                return View(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách quản lý đăng ký");
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction("Dashboard", "Admin"); // Redirect to Admin Dashboard on error
            }
        }
    }
}
