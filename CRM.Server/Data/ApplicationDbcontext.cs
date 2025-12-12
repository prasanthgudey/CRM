using CRM.Server.Models;
using CRM.Server.Models.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CRM.Server.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ✅ ONLY what YOU are responsible for
        public DbSet<AuditLog> AuditLogs { get; set; }
        // Database tables
        public DbSet<Customer> Customers { get; set; }
        //Tasks Database table
        public DbSet<TaskItem> Tasks { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<UserSession> UserSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Action).IsRequired().HasMaxLength(200);
                entity.Property(x => x.CreatedAt).IsRequired();


                //for customers
                builder.Entity<Customer>(entity =>
                {
                    entity.HasKey(c => c.CustomerId);

                    // Required
                    entity.Property(c => c.FirstName)
                          .IsRequired()
                          .HasMaxLength(200);

                    // Optional fields
                    entity.Property(c => c.MiddleName)
                          .HasMaxLength(200)
                          .IsRequired(false);     // <--- optional

                    entity.Property(c => c.PreferredName)
                          .HasMaxLength(200)
                          .IsRequired(false);     // <--- optional

                    entity.Property(c => c.Email)
                          .HasMaxLength(200);

                    entity.Property(c => c.Phone)
                          .HasMaxLength(50);

                    entity.Property(c => c.Address)
                          .HasMaxLength(500);

                    entity.Property(c => c.CreatedByUserId)
                          .HasMaxLength(450);

                    entity.Property(c => c.CreatedAt)
                          .IsRequired();
                });
            });

            //For Tasks
            // TaskItem configuration
           
builder.Entity<TaskItem>(entity =>
{
    entity.HasKey(t => t.TaskId);

    entity.Property(t => t.Title)
          .IsRequired()
          .HasMaxLength(200);

    entity.Property(t => t.Description)
          .HasMaxLength(1000);

    entity.Property(t => t.DueDate)
          .IsRequired();

    entity.Property(t => t.Priority)
          .IsRequired();

    entity.Property(t => t.State)
          .IsRequired();

    entity.Property(t => t.CreatedByUserId)
          .IsRequired();

    // 🔹 RELATIONSHIPS

    // Task → Customer (many tasks per customer)
    entity.HasOne(t => t.Customer)
          .WithMany()                       // or .WithMany(c => c.Tasks) if you add a collection
          .HasForeignKey(t => t.CustomerId)
          .OnDelete(DeleteBehavior.Cascade);

    // Task → ApplicationUser (CreatedByUserId)
    entity.HasOne(t => t.CreatedBy)
          .WithMany()                       // or .WithMany(u => u.TasksCreated) if you add a collection
          .HasForeignKey(t => t.CreatedByUserId)
          .OnDelete(DeleteBehavior.SetNull);
    // Update property name to match your model if different
});


        }

    }
}
