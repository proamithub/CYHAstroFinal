using CYHAstro.Infrastructure.Data;
using CYHAstro.Infrastructure.Identity;
using Dapper;
using Microsoft.AspNetCore.Identity;

namespace CYHAstro.Admin.Services;

public static class AdminDbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CYHAstroIdentityUser>>();
        var db = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        string[] roles = ["Admin", "Astrologer", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string adminEmail = "admin@cyhastro.com";
        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser == null)
        {
            var adminUser = new CYHAstroIdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "Super Admin",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createRes = await userManager.CreateAsync(adminUser, "Admin@CYHAstro2026!");
            if (createRes.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");

                // Ensure profile exists in UserProfiles
                using var conn = await db.CreateOpenConnectionAsync();
                var existingProfile = await conn.QuerySingleOrDefaultAsync<long?>(
                    "SELECT Id FROM dbo.UserProfiles WHERE Email = @Email", new { Email = adminEmail });

                if (!existingProfile.HasValue)
                {
                    var publicId = "adm_" + Guid.NewGuid().ToString("N")[..16];
                    const string sqlInsertProfile = @"
                        INSERT INTO dbo.UserProfiles (PublicId, IdentityId, DisplayName, Email, IsEmailVerified, IsActive, CreatedAt)
                        VALUES (@PublicId, @IdentityId, @DisplayName, @Email, 1, 1, GETUTCDATE());
                        SELECT SCOPE_IDENTITY();
                    ";

                    var profileId = await conn.ExecuteScalarAsync<long>(sqlInsertProfile, new
                    {
                        PublicId = publicId,
                        IdentityId = adminUser.Id,
                        DisplayName = "Super Admin",
                        Email = adminEmail
                    });

                    // Ensure wallet
                    const string sqlWallet = @"
                        IF NOT EXISTS (SELECT 1 FROM dbo.Wallets WHERE UserId = @UserId)
                        BEGIN
                            INSERT INTO dbo.Wallets (UserId, PaidBalance, BonusBalance, Currency, IsFrozen, CreatedAt)
                            VALUES (@UserId, 10000.00, 0.00, 'INR', 0, GETUTCDATE());
                        END
                    ";
                    await conn.ExecuteAsync(sqlWallet, new { UserId = profileId });
                }
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(existingUser, "Admin"))
            {
                await userManager.AddToRoleAsync(existingUser, "Admin");
            }
        }
    }
}
