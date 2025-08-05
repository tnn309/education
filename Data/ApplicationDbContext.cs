using EducationSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EducationSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Activity> Activities { get; set; } = null!;
        public DbSet<Teacher> Teachers { get; set; } = null!;
        public DbSet<Registration> Registrations { get; set; } = null!;
        public DbSet<Interaction> Interactions { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser entity (custom properties and relationships)
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).HasMaxLength(255);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();

                // Self-referencing relationship for Parent-Children
                entity.HasMany(u => u.Children)
                      .WithOne()
                      .HasForeignKey(s => s.ParentId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting parent if children exist

                // Relationships with other entities
                entity.HasMany(u => u.CreatedActivities)
                      .WithOne(a => a.Creator)
                      .HasForeignKey(a => a.CreatorId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.StudentRegistrations)
                      .WithOne(r => r.Student)
                      .HasForeignKey(r => r.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.ParentRegistrations)
                      .WithOne(r => r.Parent)
                      .HasForeignKey(r => r.ParentId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.Interactions)
                      .WithOne(i => i.User)
                      .HasForeignKey(i => i.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.CartItems)
                      .WithOne(ci => ci.User)
                      .HasForeignKey(ci => ci.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(u => u.Payments)
                      .WithOne(p => p.User)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.SentMessages)
                      .WithOne(m => m.Sender)
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.ReceivedMessages)
                      .WithOne(m => m.Receiver)
                      .HasForeignKey(m => m.ReceiverId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Activity entity
            builder.Entity<Activity>(entity =>
            {
                entity.HasKey(e => e.ActivityId);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Location).IsRequired().HasMaxLength(255);
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();
                entity.Property(e => e.Skills).HasMaxLength(1000);
                entity.Property(e => e.Requirements).HasMaxLength(1000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                entity.HasOne(e => e.Teacher)
                      .WithMany(t => t.Activities)
                      .HasForeignKey(e => e.TeacherId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull); // If teacher is deleted, set TeacherId to null

                entity.HasOne(e => e.Creator)
                      .WithMany(u => u.CreatedActivities)
                      .HasForeignKey(e => e.CreatorId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent deleting user if they created activities
            });

            // Configure Teacher entity
            builder.Entity<Teacher>(entity =>
            {
                entity.HasKey(e => e.TeacherId);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Specialization).HasMaxLength(255);
                entity.Property(e => e.Bio).HasMaxLength(2000);
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // Configure Registration entity
            builder.Entity<Registration>(entity =>
            {
                entity.HasKey(e => e.RegistrationId);
                entity.Property(e => e.RegistrationDate).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.AttendanceStatus).HasMaxLength(50);
                entity.Property(e => e.AmountPaid).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Activity)
                      .WithMany(a => a.Registrations)
                      .HasForeignKey(e => e.ActivityId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Student)
                      .WithMany(u => u.StudentRegistrations)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Parent)
                      .WithMany(u => u.ParentRegistrations)
                      .HasForeignKey(e => e.ParentId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User) // Compatibility property
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Interaction entity
            builder.Entity<Interaction>(entity =>
            {
                entity.HasKey(e => e.InteractionId);
                entity.Property(e => e.InteractionType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Content).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.Activity)
                      .WithMany(a => a.Interactions)
                      .HasForeignKey(e => e.ActivityId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Interactions)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CartItem entity
            builder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.CartItemId);
                entity.Property(e => e.AddedAt).IsRequired();

                entity.HasOne(e => e.Activity)
                      .WithMany(a => a.CartItems)
                      .HasForeignKey(e => e.ActivityId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.CartItems)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Payment entity
            builder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TransactionId).HasMaxLength(255);
                entity.Property(e => e.ResponseCode).HasMaxLength(255);
                entity.Property(e => e.PaymentDate).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(1000);

                entity.HasOne(e => e.Registration)
                      .WithMany(r => r.Payments)
                      .HasForeignKey(e => e.RegistrationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Payments)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Message entity
            builder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.MessageId);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.MessageType).HasMaxLength(50);

                entity.HasOne(e => e.Sender)
                      .WithMany(u => u.SentMessages)
                      .HasForeignKey(e => e.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Receiver)
                      .WithMany(u => u.ReceivedMessages)
                      .HasForeignKey(e => e.ReceiverId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Activity)
                      .WithMany(a => a.Messages)
                      .HasForeignKey(e => e.ActivityId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}