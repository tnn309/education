using EducationSystem.Data;
using EducationSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql; 
using System.Security.Claims;
using Microsoft.Extensions.Logging; 

var builder = WebApplication.CreateBuilder(args);       // tạo builder cho ứng dụng 

// thêm dịch vụ DbContext với PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// kết nối đến cơ sở dữ liệu PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString)
           .EnableSensitiveDataLogging() 
           .LogTo(Console.WriteLine, LogLevel.Information); 
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();             // Thêm dịch vụ để hiển thị lỗi phát triển trong trang web

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();     

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TeacherOnly", policy => policy.RequireRole("Teacher"));
    options.AddPolicy("ParentOnly", policy => policy.RequireRole("Parent"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
    options.AddPolicy("AdminOrTeacher", policy => policy.RequireRole("Admin", "Teacher"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Seed roles and admin user, and sample data
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>(); // Lấy logger cho Program

    try
    {
        // Apply migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("No pending migrations to apply.");
        }

        // Seed Roles
        string[] roleNames = { "Admin", "Teacher", "Parent", "Student" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation("Role '{RoleName}' created.", roleName);
            }
        }

        // Seed Admin User
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.com",
                FullName = "Admin User",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow // Đảm bảo UTC
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Admin user 'admin' created and assigned role.");
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Ensure admin user has the Admin role if they already exist
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Admin user 'admin' assigned to 'Admin' role.", adminUser.UserName);
            }
        }

        // Seed Sample Teacher (Tích hợp lại logic từ DbInitializer)
        if (!context.Teachers.Any())
        {
            var sampleTeacher = new Teacher
            {
                FullName = "Nguyễn Văn A",
                Email = "nguyenvana@example.com",
                PhoneNumber = "0901234567",
                Specialization = "Toán học",
                Experience = 10,
                Bio = "Giáo viên toán với hơn 10 năm kinh nghiệm giảng dạy.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow // Đảm bảo UTC
            };
            context.Teachers.Add(sampleTeacher);
            await context.SaveChangesAsync();
            logger.LogInformation("Sample teacher '{FullName}' created.", sampleTeacher.FullName);
        }

        // Seed Sample Activities (Tích hợp lại logic từ DbInitializer)
        if (!context.Activities.Any())
        {
            var teacher = await context.Teachers.FirstOrDefaultAsync();
            var creator = await userManager.FindByNameAsync("admin");

            if (teacher != null && creator != null)
            {
                var sampleActivity1 = new Activity
                {
                    Title = "Lớp học Toán cơ bản",
                    Description = "Khóa học dành cho học sinh tiểu học, giúp củng cố kiến thức toán cơ bản.",
                    Type = "paid",
                    Price = 500000,
                    MaxParticipants = 20,
                    MinAge = 6,
                    MaxAge = 10,
                    Location = "Phòng học A101",
                    StartDate = DateTime.Today.ToUniversalTime().Date, // Chuyển đổi sang UTC và lấy phần Date
                    EndDate = DateTime.Today.ToUniversalTime().Date,   // Chuyển đổi sang UTC và lấy phần Date
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(11, 0, 0),
                    Skills = "Giải toán, tư duy logic",
                    Requirements = "Không có",
                    TeacherId = teacher.TeacherId,
                    IsActive = true,
                    IsFull = false,
                    LikesCount = 0,
                    CommentsCount = 0,
                    Status = "Published",
                    CreatorId = creator.Id,
                    CreatedAt = DateTime.UtcNow, // Đảm bảo UTC
                    UpdatedAt = DateTime.UtcNow  // Đảm bảo UTC
                };
                context.Activities.Add(sampleActivity1);

                var sampleActivity2 = new Activity
                {
                    Title = "Câu lạc bộ Đọc sách",
                    Description = "Hoạt động miễn phí giúp học sinh yêu thích đọc sách và phát triển kỹ năng đọc hiểu.",
                    Type = "free",
                    Price = 0,
                    MaxParticipants = 30,
                    CurrentParticipants = 0,
                    MinAge = 8,
                    MaxAge = 12,
                    Location = "Thư viện trường",
                    StartDate = DateTime.Today.ToUniversalTime().Date, // Chuyển đổi sang UTC và lấy phần Date
                    EndDate = DateTime.Today.ToUniversalTime().Date,   // Chuyển đổi sang UTC và lấy phần Date
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    Skills = "Đọc hiểu, phân tích, sáng tạo",
                    Requirements = "Yêu thích đọc sách",
                    TeacherId = teacher.TeacherId,
                    IsActive = true,
                    IsFull = false,
                    LikesCount = 0,
                    CommentsCount = 0,
                    Status = "Published",
                    CreatorId = creator.Id,
                    CreatedAt = DateTime.UtcNow, // Đảm bảo UTC
                    UpdatedAt = DateTime.UtcNow  // Đảm bảo UTC
                };
                context.Activities.Add(sampleActivity2);

                await context.SaveChangesAsync();
                logger.LogInformation("Sample activities created.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
