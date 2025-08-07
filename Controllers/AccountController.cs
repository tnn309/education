using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;                            
using EducationSystem.Models;
using EducationSystem.Data;
using Microsoft.AspNetCore.Authorization;
using EducationSystem.ViewModels;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore; 

namespace EducationSystem.Controllers
{
    [AllowAnonymous]                                                                                    // cho phép người dùng không đăng nhập truy cập vào các action trong controller này
    public class AccountController : Controller
    {
        // Tiêm các dịch vụ vào dự án, readonly để không thay đổi giá trị của nó
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        // contructor để khởi tạo các dịch vụ đã tiêm
        // Nếu không có dịch vụ nào được tiêm thì sẽ ném ra ngoại lệ ArgumentNullException để chương trình vẫn chạy bình thường
        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));         // thông tin ng dùng
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));   // thông tin đăng nhập
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));         // thông tin role
            _context = context ?? throw new ArgumentNullException(nameof(context));                     // thông tin cơ sở dữ liệu
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));                        // thông tin log
        }

        [HttpGet]                                                                                       // Action để hiển thị trang đăng nhập
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;                                                          // Lưu trữ URL trả về để chuyển hướng sau khi đăng nhập thành công
            return View(new LoginViewModel());                                                          // Trả về view đăng nhập với model rỗng
        }

        [HttpPost]                                                                                      // Action để xử lý đăng nhập
        [ValidateAntiForgeryToken]                                                                      // Bảo vệ khỏi tấn công CSRF
        
        // đăng nhập người dùng 
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;                                                          // Lưu trữ URL trả về để chuyển hướng sau khi đăng nhập thành công

            if (!ModelState.IsValid)                                                                    // Kiểm tra tính hợp lệ của model
            {
                return View(model);                                                                     // Nếu không hợp lệ, trả về view đăng nhập với model hiện tại
            }

            try
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);   // Thử đăng nhập với tên đăng nhập, mật khẩu và tùy chọn ghi nhớ đăng nhập

                if (result.Succeeded)                                                                   // đăng nhập thành công                                        
                {
                    _logger.LogInformation("Người dùng {Username} đã đăng nhập thành công.", model.Username);
                    return RedirectToLocal(returnUrl);
                }

                if (result.IsLockedOut)                                                                 // Tài khoản bị khóa                                     
                {
                    _logger.LogWarning("Tài khoản {Username} đã bị khóa.", model.Username);
                    ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                }
                else                                                                                    // đăng nhập thất bại                           
                {
                    ModelState.AddModelError("", "Đăng nhập thất bại. Vui lòng kiểm tra tên đăng nhập và mật khẩu.");
                }
            }
            catch (Exception ex)                                                                        // ngoại lệ và ghi log lỗi
            {
                _logger.LogError(ex, "Lỗi xảy ra khi đăng nhập cho người dùng {Username}", model.Username);
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
            }

            return View(model);
        }

        [HttpGet]                                                                                        // Action để hiển thị trang đăng ký
        public IActionResult Register()
        {
            return View(new RegisterViewModel());                                                         
        }

        [HttpPost]                                                                                      // Action để xử lý đăng ký người dùng mới
        [ValidateAntiForgeryToken]
        

        // đăng kí người dùng mới
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = new ApplicationUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth.HasValue ? DateTime.SpecifyKind(model.DateOfBirth.Value, DateTimeKind.Utc) : (DateTime?)null,
                    Address = model.Address,
                    CreatedAt = DateTime.UtcNow
                };

                if (model.Role == "Student" && !string.IsNullOrEmpty(model.ParentUsername))
                {
                    var parent = await _userManager.Users
                                 .FirstOrDefaultAsync(u => u.UserName == model.ParentUsername || u.Email == model.ParentUsername);

                    if (parent == null || !await _userManager.IsInRoleAsync(parent, "Parent"))
                    {
                        ModelState.AddModelError("ParentUsername", "Phụ huynh không tồn tại hoặc không phải là vai trò phụ huynh.");
                        return View(model);
                    }
                    user.ParentId = parent.Id;
                }
                else if (model.Role == "Parent")
                {
                    user.ParentId = null; // Ensure ParentId is null for parents
                }

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Người dùng {Username} đã đăng ký thành công.", model.Username);

                    // Ensure roles exist
                    if (!await _roleManager.RoleExistsAsync("Admin")) await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    if (!await _roleManager.RoleExistsAsync("Teacher")) await _roleManager.CreateAsync(new IdentityRole("Teacher"));
                    if (!await _roleManager.RoleExistsAsync("Parent")) await _roleManager.CreateAsync(new IdentityRole("Parent"));
                    if (!await _roleManager.RoleExistsAsync("Student")) await _roleManager.CreateAsync(new IdentityRole("Student"));

                    await _userManager.AddToRoleAsync(user, model.Role);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("Người dùng {Username} đã tự động đăng nhập sau khi đăng ký.", model.Username);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi đăng ký cho người dùng {Username}", model.Username);
                ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("Người dùng {Username} đã đăng xuất.", User?.Identity?.Name ?? "Unknown");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi đăng xuất.");
                TempData["Error"] = "Đã xảy ra lỗi khi đăng xuất. Vui lòng thử lại.";
                return RedirectToAction("Index", "Home");
            }
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
