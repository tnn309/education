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
    public class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegistrationController> _logger;

        public RegistrationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<RegistrationController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> MyRegistrations()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Login", "Account");
            }

            var registrations = await _context.Registrations
                .Include(r => r.Activity)
                .Include(r => r.Student)
                .Include(r => r.Parent)
                .Where(r => r.StudentId == userId || r.ParentId == userId) // Get registrations for current user (as student or parent)
                .OrderByDescending(r => r.RegistrationDate)
                .ToListAsync();

            return View(registrations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var registration = await _context.Registrations
                .Include(r => r.Activity)
                .FirstOrDefaultAsync(r => r.RegistrationId == id && (r.StudentId == userId || r.ParentId == userId));

            if (registration == null)
            {
                TempData["Error"] = "Không tìm thấy đăng ký hoặc bạn không có quyền hủy.";
                return RedirectToAction(nameof(MyRegistrations));
            }

            if (registration.Status == "Cancelled" || registration.Status == "Rejected")
            {
                TempData["Error"] = "Đăng ký này đã bị hủy hoặc từ chối rồi.";
                return RedirectToAction(nameof(MyRegistrations));
            }

            // Allow cancellation only if activity start date is in the future
            if (registration.Activity != null && registration.Activity.StartDate <= DateTime.UtcNow.Date)
            {
                TempData["Error"] = "Không thể hủy đăng ký cho hoạt động đã bắt đầu hoặc kết thúc.";
                return RedirectToAction(nameof(MyRegistrations));
            }

            registration.Status = "Cancelled";
            registration.Notes = "Hủy bởi người dùng.";

            // If it was an approved free activity, decrement participant count
            if (registration.Activity != null && registration.Activity.Type == "free" && registration.Status == "Approved")
            {
                registration.Activity.CurrentParticipants = Math.Max(0, registration.Activity.CurrentParticipants - 1);
                registration.Activity.IsFull = false; // No longer full if participants decrease
                registration.Activity.Status = "Published"; // Reset status from "Full" if applicable
            }
            // For paid activities, handle refund logic if needed (not implemented here)
            // For simplicity, we just mark as cancelled.

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đăng ký đã được hủy thành công.";
            _logger.LogInformation("Người dùng {UserId} đã hủy đăng ký {RegistrationId}", userId, id);
            return RedirectToAction(nameof(MyRegistrations));
        }
    }
}
