using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CynAstro.Infrastructure.Identity;

/// <summary>
/// EF Core DbContext used ONLY for ASP.NET Core Identity.
/// Business data access uses Dapper — not this context.
/// </summary>
public sealed class CynAstroIdentityDbContext : IdentityDbContext<CynAstroIdentityUser>
{
    public CynAstroIdentityDbContext(DbContextOptions<CynAstroIdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Map to our custom table names
        builder.Entity<CynAstroIdentityUser>().ToTable("AspNetUsers");
        builder.Entity<IdentityRole>().ToTable("AspNetRoles");
        builder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens");
    }
}

/// <summary>
/// Extended Identity user. Business profile is in UserProfiles / ApplicationUser tables.
/// </summary>
public sealed class CynAstroIdentityUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
