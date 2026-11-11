using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Auth;
using CYHAstro.Core.DTOs.User;

namespace CYHAstro.Core.Interfaces.Services;

public interface IProfileService
{
    Task<Result<UserProfileDto>> GetProfileAsync(long userId, CancellationToken ct = default);
    Task<Result<UserProfileDto>> UpdateProfileAsync(long userId, UserProfileUpdateDto request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<BirthProfileDto>>> GetBirthProfilesAsync(long userId, CancellationToken ct = default);
    Task<Result<BirthProfileDto>> GetBirthProfileByIdAsync(long profileId, long userId, CancellationToken ct = default);
    Task<Result<BirthProfileDto>> CreateBirthProfileAsync(long userId, CreateBirthProfileDto request, CancellationToken ct = default);
    Task<Result<BirthProfileDto>> UpdateBirthProfileAsync(long profileId, long userId, UpdateBirthProfileDto request, CancellationToken ct = default);
    Task<Result> DeleteBirthProfileAsync(long profileId, long userId, CancellationToken ct = default);
    Task<Result<UserPreferenceDto>> GetPreferencesAsync(long userId, CancellationToken ct = default);
    Task<Result<UserPreferenceDto>> UpdatePreferencesAsync(long userId, UserPreferenceDto request, CancellationToken ct = default);
}
