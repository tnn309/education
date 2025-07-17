using EducationSystem.Data;
using EducationSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql; // Ensure this is present for PostgreSQL
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configure PostgreSQL connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // For simplicity, disable email confirmation
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Required for Identity UI

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TeacherOnly", policy => policy.RequireRole("Teacher"));
    options.AddPolicy("ParentOnly", policy => policy.RequireRole("Parent"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
    options.AddPolicy("AdminOrTeacher", policy => policy.RequireRole("Admin", "Teacher"));
});

// Configure cookie settings for Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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
app.MapRazorPages(); // Required for Identity UI

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Apply migrations
        context.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");

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
                CreatedAt = DateTime.UtcNow
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

        // Seed Sample Teachers
        var teacher1 = await context.Teachers.FirstOrDefaultAsync(t => t.Email == "teacher1@example.com");
        if (teacher1 == null)
        {
            teacher1 = new Teacher
            {
                FullName = "Nguyễn Thị B",
                Email = "teacher1@example.com",
                PhoneNumber = "0912345678",
                Specialization = "Tiếng Anh",
                Experience = 7,
                Bio = "Giáo viên tiếng Anh với phương pháp giảng dạy sáng tạo.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Teachers.Add(teacher1);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded teacher: {FullName}", teacher1.FullName);
        }

        var teacher2 = await context.Teachers.FirstOrDefaultAsync(t => t.Email == "teacher2@example.com");
        if (teacher2 == null)
        {
            teacher2 = new Teacher
            {
                FullName = "Trần Văn C",
                Email = "teacher2@example.com",
                PhoneNumber = "0987654321",
                Specialization = "Vật lý",
                Experience = 10,
                Bio = "Chuyên gia vật lý, giảng dạy từ cơ bản đến nâng cao.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Teachers.Add(teacher2);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded teacher: {FullName}", teacher2.FullName);
        }

        // Seed Sample Users (Parent and Student)
        var parentUser = await userManager.FindByNameAsync("parent1");
        if (parentUser == null)
        {
            parentUser = new ApplicationUser
            {
                UserName = "parent1",
                Email = "parent1@example.com",
                FullName = "Phụ huynh A",
                DateOfBirth = DateTime.SpecifyKind(new DateTime(1980, 5, 15), DateTimeKind.Utc),
                Address = "123 Đường ABC, Quận 1",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(parentUser, "Parent123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(parentUser, "Parent");
                logger.LogInformation("Parent user 'parent1' created and assigned role.");
            }
        }

        var studentUser = await userManager.FindByNameAsync("student1");
        if (studentUser == null)
        {
            studentUser = new ApplicationUser
            {
                UserName = "student1",
                Email = "student1@example.com",
                FullName = "Học sinh X",
                DateOfBirth = DateTime.SpecifyKind(new DateTime(2010, 8, 20), DateTimeKind.Utc),
                Address = "123 Đường ABC, Quận 1",
                ParentId = parentUser?.Id, // Link to parent
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(studentUser, "Student123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, "Student");
                logger.LogInformation("Student user 'student1' created and assigned role.");
            }
        }

        // Seed Sample Activities
        var activity1 = await context.Activities.FirstOrDefaultAsync(a => a.Title == "Lớp học Toán cơ bản");
        if (activity1 == null)
        {
            activity1 = new Activity
            {
                Title = "Lớp học Toán cơ bản",
                Description = "Khóa học toán cơ bản dành cho học sinh tiểu học, giúp củng cố kiến thức nền tảng.",
                ImageUrl = "/images/math_class.jpg",
                Type = "paid",
                Price = 500000,
                MaxParticipants = 30,
                MinAge = 6,
                MaxAge = 10,
                Skills = "Tư duy logic, Giải quyết vấn đề",
                Requirements = "Không yêu cầu kinh nghiệm trước",
                Location = "Phòng học A101",
                StartDate = DateTime.UtcNow.AddDays(7).Date,
                EndDate = DateTime.UtcNow.AddDays(14).Date,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                IsActive = true,
                Status = "Published",
                TeacherId = teacher1?.TeacherId,
                CreatedBy = adminUser?.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Activities.Add(activity1);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded activity: {Title}", activity1.Title);
        }

        var activity2 = await context.Activities.FirstOrDefaultAsync(a => a.Title == "Lớp học Tiếng Anh giao tiếp");
        if (activity2 == null)
        {
            activity2 = new Activity
            {
                Title = "Lớp học Tiếng Anh giao tiếp",
                Description = "Khóa học tập trung vào kỹ năng nghe nói, giúp học sinh tự tin giao tiếp.",
                ImageUrl = "/images/english_class.jpg",
                Type = "free",
                Price = 0,
                MaxParticipants = 25,
                MinAge = 8,
                MaxAge = 14,
                Skills = "Giao tiếp, Nghe, Nói",
                Requirements = "Có kiến thức tiếng Anh cơ bản",
                Location = "Phòng học B203",
                StartDate = DateTime.UtcNow.AddDays(10).Date,
                EndDate = DateTime.UtcNow.AddDays(20).Date,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(16, 0, 0),
                IsActive = true,
                Status = "Published",
                TeacherId = teacher1?.TeacherId,
                CreatedBy = adminUser?.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Activities.Add(activity2);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded activity: {Title}", activity2.Title);
        }

        var activity3 = await context.Activities.FirstOrDefaultAsync(a => a.Title == "Khóa học Lập trình Scratch");
        if (activity3 == null)
        {
            activity3 = new Activity
            {
                Title = "Khóa học Lập trình Scratch",
                Description = "Giới thiệu lập trình cho trẻ em thông qua nền tảng Scratch trực quan.",
                ImageUrl = "/images/scratch_coding.jpg",
                Type = "paid",
                Price = 750000,
                MaxParticipants = 15,
                MinAge = 7,
                MaxAge = 12,
                Skills = "Lập trình, Sáng tạo, Giải quyết vấn đề",
                Requirements = "Máy tính có kết nối internet",
                Location = "Phòng Lab C101",
                StartDate = DateTime.UtcNow.AddDays(15).Date,
                EndDate = DateTime.UtcNow.AddDays(25).Date,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                IsActive = true,
                Status = "Published",
                TeacherId = teacher2?.TeacherId,
                CreatedBy = adminUser?.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Activities.Add(activity3);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded activity: {Title}", activity3.Title);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
