using Microsoft.AspNetCore.Mvc;
using EducationSystem.Data;
using EducationSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using EducationSystem.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq; // Added for .Any()
using Microsoft.AspNetCore.Authentication;

namespace EducationSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> Index(string filter = "all", int page = 1, string search = "", string sortBy = "newest")
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
                            // Filter by registrations where the current user is the student or the parent
                            query = query.Where(a => a.Registrations.Any(r => (r.StudentId == userId || r.ParentId == userId) && r.Status == "Approved"));
                        }
                        else
                        {
                            query = query.Where(a => false); // No activities if not authenticated
                        }
                        break;
                }

                query = sortBy switch
                {
                    "oldest" => query.OrderBy(a => a.CreatedAt),
                    "price_low" => query.OrderBy(a => a.Price),
                    "price_high" => query.OrderByDescending(a => a.Price),
                    "start_date" => query.OrderBy(a => a.StartDate),
                    "popular" => query.OrderByDescending(a => a.Interactions.Count(i => i.InteractionType == "Like")), // Order by likes count
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
                _logger.LogError(ex, "Lỗi khi tải trang chủ với filter {Filter}", filter);
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}
