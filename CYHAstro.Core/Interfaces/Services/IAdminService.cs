using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Admin;

namespace CYHAstro.Core.Interfaces.Services;

public interface IAdminService
{
    Task<Result<AdminDashboardDto>> GetDashboardStatsAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<AdminAstrologerDto>>> GetAstrologersAsync(string? status = null, string? search = null, CancellationToken ct = default);
    Task<Result<AdminAstrologerDto>> GetAstrologerByIdAsync(long id, CancellationToken ct = default);
    Task<Result> UpdateAstrologerStatusAsync(AdminUpdateAstrologerDto request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AdminRechargePackageDto>>> GetRechargePackagesAsync(CancellationToken ct = default);
    Task<Result> SaveRechargePackageAsync(AdminRechargePackageDto package, CancellationToken ct = default);
    Task<Result> TogglePackageStatusAsync(long packageId, bool isActive, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AdminTransactionDto>>> GetPlatformTransactionsAsync(string? type = null, int limit = 50, CancellationToken ct = default);
}
