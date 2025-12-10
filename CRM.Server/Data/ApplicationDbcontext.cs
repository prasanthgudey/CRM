using CRM.Server.Models;
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
        }

    }
}
