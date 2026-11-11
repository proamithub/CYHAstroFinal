using CynAstro.Core.Constants;

namespace CynAstro.Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddCynAstroAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Role policies
            options.AddPolicy("RequireUser", p => p.RequireRole(AppConstants.Roles.User));
            options.AddPolicy("RequireAstrologer", p => p.RequireRole(AppConstants.Roles.Astrologer));
            options.AddPolicy("RequireAdmin", p => p.RequireRole(AppConstants.Roles.Admin, AppConstants.Roles.SuperAdmin));
            options.AddPolicy("RequireSuperAdmin", p => p.RequireRole(AppConstants.Roles.SuperAdmin));
            options.AddPolicy("RequireSupport", p => p.RequireRole(AppConstants.Roles.Support, AppConstants.Roles.Admin, AppConstants.Roles.SuperAdmin));

            // Any authenticated user (all roles)
            options.AddPolicy("RequireAuthenticated", p => p.RequireAuthenticatedUser());
        });

        return services;
    }
}
