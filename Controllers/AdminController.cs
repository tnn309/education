using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EducationSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EducationSystem.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;

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
                // Calculate total revenue from completed payments
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
                var users = await _userManager.Users
                    .Include(u => u.Children) // thêm thông tin về trẻ em nếu cần
                    .ToListAsync();
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
                var teachers = await _context.Teachers
                    .Include(t => t.Activities) // Include activities if needed for display
                    .ToListAsync();
                return View(teachers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách giáo viên");
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher([Bind("FullName,Email,PhoneNumber,Specialization,Experience,Bio")] Teacher teacher)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu giáo viên không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Teachers));
            }

            try
            {
                teacher.CreatedAt = DateTime.UtcNow;
                teacher.IsActive = true; // Default to active
                _context.Teachers.Add(teacher);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm giáo viên mới thành công!";
                return RedirectToAction(nameof(Teachers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm giáo viên mới: {FullName}", teacher.FullName);
                TempData["Error"] = "Đã xảy ra lỗi khi thêm giáo viên. Vui lòng thử lại.";
                return RedirectToAction(nameof(Teachers));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            try
            {
                var teacher = await _context.Teachers.FindAsync(id);
                if (teacher == null)
                {
                    TempData["Error"] = "Không tìm thấy giáo viên.";
                    return RedirectToAction(nameof(Teachers));
                }

                _context.Teachers.Remove(teacher);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa giáo viên thành công.";
                return RedirectToAction(nameof(Teachers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa giáo viên với ID: {Id}", id);
                TempData["Error"] = "Đã xảy ra lỗi khi xóa giáo viên. Vui lòng thử lại.";
                return RedirectToAction(nameof(Teachers));
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
                    .Include(r => r.User) // Ensure User is loaded if used
                    .Include(r => r.Activity)
                    .OrderByDescending(r => r.RegistrationDate)
                    .ToListAsync();
                return View("~/Views/Registration/ManageRegistrations.cshtml", registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách quản lý đăng ký");
                TempData["Error"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return RedirectToAction("Dashboard", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRegistration(int id)
        {
            try
            {
                var registration = await _context.Registrations
                    .Include(r => r.Activity)
                    .FirstOrDefaultAsync(r => r.RegistrationId == id);

                if (registration == null)
                {
                    TempData["Error"] = "Không tìm thấy đăng ký.";
                    return RedirectToAction(nameof(ManageRegistrations));
                }

                if (registration.Status != "Pending")
                {
                    TempData["Error"] = "Đăng ký này đã được xử lý.";
                    return RedirectToAction(nameof(ManageRegistrations));
                }

                // Kiểm tra xem hoạt động có còn chỗ không
                if (registration.Activity != null)
                {
                    // Tăng số lượng người tham gia hiện tại trước khi kiểm tra IsFull
                    // Điều này đảm bảo rằng IsFull được cập nhật chính xác sau khi duyệt
                    registration.Activity.CurrentParticipants++; 

                    if (registration.Activity.CurrentParticipants > registration.Activity.MaxParticipants)
                    {
                        // Nếu vượt quá số lượng tối đa sau khi tăng, thì không duyệt và hoàn tác
                        registration.Activity.CurrentParticipants--; // Hoàn tác
                        TempData["Error"] = "Hoạt động đã đầy, không thể duyệt thêm đăng ký.";
                        await _context.SaveChangesAsync(); // Lưu lại trạng thái đã hoàn tác
                        return RedirectToAction(nameof(ManageRegistrations));
                    }

                    // Cập nhật trạng thái IsFull của hoạt động
                    if (registration.Activity.CurrentParticipants >= registration.Activity.MaxParticipants)
                    {
                        registration.Activity.IsFull = true;
                        registration.Activity.Status = "Full"; // Cập nhật trạng thái hoạt động thành "Full"
                    }
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
                if (registration == null)
                {
                    TempData["Error"] = "Không tìm thấy đăng ký.";
                    return RedirectToAction(nameof(ManageRegistrations));
                }

                if (registration.Status != "Pending")
                {
                    TempData["Error"] = "Đăng ký này đã được xử lý.";
                    return RedirectToAction(nameof(ManageRegistrations));
                }

                registration.Status = "Rejected";
                registration.Notes = "Từ chối bởi quản trị viên.";
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