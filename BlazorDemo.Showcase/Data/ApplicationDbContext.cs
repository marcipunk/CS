using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazorDemo.Showcase.Data {
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options) {
        // Add your business DbSets here:
        // public DbSet<Order> Orders => Set<Order>();
        // public DbSet<Customer> Customers => Set<Customer>();

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            // Default schema for your business tables
            builder.HasDefaultSchema("dbo");

            // Place Identity tables in a dedicated 'auth' schema
            builder.Entity<ApplicationUser>().ToTable("Users", "auth");
            builder.Entity<IdentityRole>().ToTable("Roles", "auth");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "auth");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "auth");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "auth");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "auth");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "auth");
        }
    }
}
