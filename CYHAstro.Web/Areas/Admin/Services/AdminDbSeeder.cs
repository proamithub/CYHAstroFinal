using CYHAstro.Infrastructure.Data;
using CYHAstro.Infrastructure.Identity;
using Dapper;
using Microsoft.AspNetCore.Identity;

namespace CYHAstro.Web.Areas.Admin.Services;

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
                            INSERT INTO dbo.Wallets (UserId, PaidBalance, BonusBalance, CurrencyCode, CreatedAt)
                            VALUES (@UserId, 10000.00, 0.00, 'INR', GETUTCDATE());
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

        // ── Seed Default Astrologer User ──
        const string astroEmail = "astrologer@cyhastro.com";
        var existingAstro = await userManager.FindByEmailAsync(astroEmail);
        long astroProfileId = 0;

        if (existingAstro == null)
        {
            var astroUser = new CYHAstroIdentityUser
            {
                UserName = astroEmail,
                Email = astroEmail,
                DisplayName = "Acharya Raman Shastri",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createAstroRes = await userManager.CreateAsync(astroUser, "Astro@CYHAstro2026!");
            if (createAstroRes.Succeeded)
            {
                await userManager.AddToRoleAsync(astroUser, "Astrologer");

                using var conn = await db.CreateOpenConnectionAsync();
                var publicId = "usr_" + Guid.NewGuid().ToString("N")[..16];
                const string sqlInsertProfile = @"
                    INSERT INTO dbo.UserProfiles (PublicId, IdentityId, DisplayName, Email, IsEmailVerified, IsActive, CreatedAt)
                    VALUES (@PublicId, @IdentityId, @DisplayName, @Email, 1, 1, GETUTCDATE());
                    SELECT SCOPE_IDENTITY();
                ";

                astroProfileId = await conn.ExecuteScalarAsync<long>(sqlInsertProfile, new
                {
                    PublicId = publicId,
                    IdentityId = astroUser.Id,
                    DisplayName = "Acharya Raman Shastri",
                    Email = astroEmail
                });
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(existingAstro, "Astrologer"))
            {
                await userManager.AddToRoleAsync(existingAstro, "Astrologer");
            }

            using var conn = await db.CreateOpenConnectionAsync();
            var pId = await conn.QuerySingleOrDefaultAsync<long?>(
                "SELECT TOP 1 Id FROM dbo.UserProfiles WHERE IdentityId = @IdentityId OR Email = @Email",
                new { IdentityId = existingAstro.Id, Email = astroEmail });

            if (pId.HasValue)
            {
                astroProfileId = pId.Value;
                await conn.ExecuteAsync(
                    "UPDATE dbo.UserProfiles SET IdentityId = @IdentityId, Email = @Email WHERE Id = @Id",
                    new { IdentityId = existingAstro.Id, Email = astroEmail, Id = astroProfileId });
            }
            else
            {
                var publicId = "usr_" + Guid.NewGuid().ToString("N")[..16];
                const string sqlInsertProfile = @"
                    INSERT INTO dbo.UserProfiles (PublicId, IdentityId, DisplayName, Email, IsEmailVerified, IsActive, CreatedAt)
                    VALUES (@PublicId, @IdentityId, @DisplayName, @Email, 1, 1, GETUTCDATE());
                    SELECT SCOPE_IDENTITY();
                ";

                astroProfileId = await conn.ExecuteScalarAsync<long>(sqlInsertProfile, new
                {
                    PublicId = publicId,
                    IdentityId = existingAstro.Id,
                    DisplayName = "Acharya Raman Shastri",
                    Email = astroEmail
                });
            }
        }

        // Link profile to Astrologers record
        if (astroProfileId > 0)
        {
            using var conn = await db.CreateOpenConnectionAsync();
            const string sqlEnsureAstro = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.Astrologers WHERE UserId = @UserId)
                BEGIN
                    IF EXISTS (SELECT 1 FROM dbo.Astrologers WHERE PublicId = 'ast_raman_01')
                    BEGIN
                        UPDATE dbo.Astrologers SET UserId = @UserId WHERE PublicId = 'ast_raman_01';
                    END
                    ELSE
                    BEGIN
                        INSERT INTO dbo.Astrologers (
                            PublicId, UserId, DisplayName, Specializations, Languages, Bio,
                            ChatRatePerMinute, AudioRatePerMinute, VideoRatePerMinute, Rating, TotalReviews,
                            TotalConsultations, ExperienceYears, IsAvailable, IsVerified, IsActive,
                            ProfilePhotoUrl, CommissionRatePct, CreatedAt
                        )
                        VALUES (
                            'ast_raman_01', @UserId, 'Acharya Raman Shastri',
                            '[""Vedic Astrology"", ""Kundli Matching"", ""Vastu Shastra""]',
                            '[""Hindi"", ""English"", ""Sanskrit""]',
                            'Gold Medalist in Vedic Jyotish with 18 years of experience. Career, marriage & kundli expert.',
                            25.00, 35.00, 45.00, 4.96, 50,
                            120, 18, 1, 1, 1,
                            'https://images.unsplash.com/photo-1544717305-2782549b5136?auto=format&fit=crop&q=80&w=400',
                            20.00, GETUTCDATE()
                        );
                    END
                END

                IF NOT EXISTS (SELECT 1 FROM dbo.Wallets WHERE UserId = @UserId)
                BEGIN
                    INSERT INTO dbo.Wallets (UserId, PaidBalance, BonusBalance, CurrencyCode, CreatedAt)
                    VALUES (@UserId, 5000.00, 0.00, 'INR', GETUTCDATE());
                END
            ";
            await conn.ExecuteAsync(sqlEnsureAstro, new { UserId = astroProfileId });
        }
    }
}
