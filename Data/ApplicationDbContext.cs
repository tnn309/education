using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EducationSystem.Models;

namespace EducationSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Interaction> Interactions { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ApplicationUser
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("AspNetUsers"); // Explicitly map to AspNetUsers table
                entity.Property(u => u.FullName).HasMaxLength(100);
                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnType("timestamp with time zone");
                entity.Property(u => u.UpdatedAt)
                    .HasColumnType("timestamp with time zone");
                
                // Self-referencing relationship for Parent-Child
                entity.HasOne(u => u.Parent)
                    .WithMany(u => u.Children)
                    .HasForeignKey(u => u.ParentId)
                    .OnDelete(DeleteBehavior.SetNull); // ParentId can be null
            });

            // Configure Teacher
            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.ToTable("Teachers");
                entity.HasKey(t => t.TeacherId);
                entity.Property(t => t.FullName).IsRequired().HasMaxLength(100);
                entity.Property(t => t.Email).HasMaxLength(256);
                entity.Property(t => t.PhoneNumber).HasMaxLength(20);
                entity.Property(t => t.Specialization).HasMaxLength(100);
                entity.Property(t => t.IsActive).HasDefaultValue(true);
                entity.Property(t => t.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnType("timestamp with time zone");
                entity.Property(t => t.UpdatedAt)
                    .HasColumnType("timestamp with time zone");
            });

            // Configure Activity
            modelBuilder.Entity<Activity>(entity =>
            {
                entity.ToTable("Activities");
                entity.HasKey(a => a.ActivityId);
                entity.Property(a => a.Title).IsRequired().HasMaxLength(255);
                entity.Property(a => a.Description).IsRequired();
                entity.Property(a => a.ImageUrl).HasMaxLength(500);
                entity.Property(a => a.Type).IsRequired().HasMaxLength(50);
                entity.Property(a => a.Price).HasColumnType("decimal(10,2)");
                entity.Property(a => a.Location).HasMaxLength(255);
                entity.Property(a => a.Status).HasMaxLength(50).HasDefaultValue("Published"); // Default status
                entity.Property(a => a.StartDate).HasColumnType("date");
                entity.Property(a => a.EndDate).HasColumnType("date");
                entity.Property(a => a.StartTime).HasColumnType("time");
                entity.Property(a => a.EndTime).HasColumnType("time");
                entity.Property(a => a.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnType("timestamp with time zone");
                entity.Property(a => a.UpdatedAt)
                    .HasColumnType("timestamp with time zone");
                
                entity.HasOne(a => a.Teacher)
                    .WithMany(t => t.Activities)
                    .HasForeignKey(a => a.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull); // TeacherId can be null
                
                entity.HasOne(a => a.Creator)
                    .WithMany(u => u.CreatedActivities)
                    .HasForeignKey(a => a.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull); // CreatedBy can be null
            });

            // Configure Registration
            modelBuilder.Entity<Registration>(entity =>
            {
                entity.ToTable("Registrations");
                entity.HasKey(r => r.RegistrationId);
                entity.Property(r => r.Status).HasMaxLength(50).HasDefaultValue("Pending");
                entity.Property(r => r.PaymentStatus).HasMaxLength(50).HasDefaultValue("Unpaid");
                entity.Property(r => r.AttendanceStatus).HasMaxLength(50).HasDefaultValue("Registered");
                entity.Property(r => r.RegistrationDate)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnType("timestamp with time zone");
                
                // Relationship for the user who initiated the registration (UserId)
                entity.HasOne(r => r.User)
                    .WithMany(u => u.Registrations) // Assuming ApplicationUser has a Registrations collection
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade); 
                
                // Relationship for the actual student being registered (StudentId)
                entity.HasOne(r => r.Student)
                    .WithMany(u => u.StudentRegistrations)
                    .HasForeignKey(r => r.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Relationship for the parent who registered the student (ParentId)
                entity.HasOne(r => r.Parent)
                    .WithMany(u => u.ParentRegistrations)
                    .HasForeignKey(r => r.ParentId)
                    .OnDelete(DeleteBehavior.SetNull); // ParentId can be null
                
                entity.HasOne(r => r.Activity)
                    .WithMany(a => a.Registrations)
                    .HasForeignKey(r => r.ActivityId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Unique constraint to prevent duplicate registrations for the same student and activity
                // Đã sửa: Thêm dấu ngoặc kép cho tên cột "Status" để PostgreSQL nhận diện đúng
                entity.HasIndex(r => new { r.StudentId, r.ActivityId })
                    .IsUnique()
                    .HasFilter("\"Status\" != 'Cancelled'") // <-- Đã sửa ở đây
                    .HasDatabaseName("IX_Registrations_StudentId_ActivityId");
            });

            // Configure Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.HasKey(p => p.PaymentId);
                entity.Property(p => p.Amount).HasColumnType("decimal(10,2)");
                entity.Property(p => p.PaymentMethod).HasMaxLength(50);
                entity.Property(p => p.TransactionId).HasMaxLength(255);
                entity.Property(p => p.PaymentStatus).HasMaxLength(50).HasDefaultValue("Pending");
                entity.Property(p => p.ResponseCode).HasMaxLength(50);
                entity.Property(p => p.PaymentDate)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnType("timestamp with time zone");
                
                entity.HasOne(p => p.Registration)
                    .WithMany(r => r.Payments)
                    .HasForeignKey(p => p.RegistrationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Message
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Messages");
                entity.HasKey(m => m.MessageId);
                entity.Property(m => m.Subject).HasMaxLength(255);
                entity.Property(m => m.Content).IsRequired();
                entity.Property(m => m.MessageType).HasMaxLength(50).HasDefaultValue("General");
                entity.Property(m => m.IsRead).HasDefaultValue(false);
                entity.Property(m => m.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnType("timestamp with time zone");
                
                entity.HasOne(m => m.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(m => m.Recipient)
                    .WithMany(u => u.ReceivedMessages)
                    .HasForeignKey(m => m.RecipientId)
                    .OnDelete(DeleteBehavior.SetNull); // RecipientId can be null
                
                entity.HasOne(m => m.RelatedActivity)
                    .WithMany(a => a.Messages)
                    .HasForeignKey(m => m.RelatedActivityId)
                    .OnDelete(DeleteBehavior.SetNull); // RelatedActivityId can be null
            });

            // Configure Interaction
            modelBuilder.Entity<Interaction>(entity =>
            {
                entity.ToTable("Interactions");
                entity.HasKey(i => i.InteractionId);
                entity.Property(i => i.InteractionType).IsRequired().HasMaxLength(50);
                entity.Property(i => i.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnType("timestamp with time zone");
                
                entity.HasOne(i => i.User)
                    .WithMany(u => u.Interactions)
                    .HasForeignKey(i => i.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(i => i.Activity)
                    .WithMany(a => a.Interactions)
                    .HasForeignKey(i => i.ActivityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure CartItem
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("CartItems");
                entity.HasKey(c => c.CartItemId);
                entity.Property(c => c.IsPaid).HasDefaultValue(false);
                entity.Property(c => c.AddedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnType("timestamp with time zone");
                
                entity.HasOne(c => c.User)
                    .WithMany(u => u.CartItems)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict); // Restrict deletion if cart items exist
                
                entity.HasOne(c => c.Activity)
                    .WithMany(a => a.CartItems)
                    .HasForeignKey(c => c.ActivityId)
                    .OnDelete(DeleteBehavior.Restrict); // Restrict deletion if cart items exist

                // Unique constraint to prevent duplicate cart items for the same user and activity
                entity.HasIndex(c => new { c.UserId, c.ActivityId })
                    .IsUnique()
                    .HasDatabaseName("IX_CartItems_UserId_ActivityId");
            });
        }
    }
}
