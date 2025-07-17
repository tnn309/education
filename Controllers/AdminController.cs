using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EducationSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EducationSystem.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace EducationSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                ViewBag.TotalActivities = await _context.Activities.CountAsync();
                ViewBag.PublishedActivities = await _context.Activities.CountAsync(a => a.Status == "Published");
                ViewBag.TotalUsers = await _userManager.Users.CountAsync();
                ViewBag.TotalRegistrations = await _context.Registrations.CountAsync();
                ViewBag.PendingRegistrations = await _context.Registrations.CountAsync(r => r.Status == "Pending");
                ViewBag.TotalTeachers = await _context.Teachers.CountAsync();
                ViewBag.TotalRevenue = await _context.Payments.Where(p => p.PaymentStatus == "Completed").SumAsync(p => p.Amount);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải bảng điều khiển");
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách người dùng");
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Teachers()
        {
            try
            {
                var teachers = await _context.Teachers.ToListAsync();
                return View(teachers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách giáo viên");
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpGet]
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
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRegistration(int id)
        {
            try
            {
                var registration = await _context.Registrations.FindAsync(id);
                if (registration == null || registration.Status != "Pending")
                {
                    TempData["Error"] = "Đăng ký không hợp lệ hoặc đã được xử lý.";
                    return RedirectToAction(nameof(ManageRegistrations));
                }

                registration.Status = "Approved";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đăng ký {RegistrationId} đã được phê duyệt bởi Admin", id);
                TempData["Success"] = "Đăng ký đã được phê duyệt.";
                return RedirectToAction(nameof(ManageRegistrations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phê duyệt đăng ký {RegistrationId}", id);
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction(nameof(ManageRegistrations));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineRegistration(int id)
        {
            try
            {
                var registration = await _context.Registrations.FindAsync(id);
                if (registration == null || registration.Status != "Pending")
                {
                    TempData["Error"] = "Đăng ký không hợp lệ hoặc đã được xử lý.";
                    return RedirectToAction(nameof(ManageRegistrations));
                }

                _context.Registrations.Remove(registration);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đăng ký {RegistrationId} đã bị từ chối bởi Admin", id);
                TempData["Success"] = "Đăng ký đã bị từ chối.";
                return RedirectToAction(nameof(ManageRegistrations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi từ chối đăng ký {RegistrationId}", id);
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction(nameof(ManageRegistrations));
            }
        }
    }
}