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
    Task<Result<IReadOnlyList<SettlementDto>>> GetAllSettlementsAsync(string? status = null, CancellationToken ct = default);
    Task<Result> ProcessSettlementAsync(long settlementId, string status, string? payoutReference, string? remarks, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AdminUserDto>>> GetUsersAsync(string? search = null, CancellationToken ct = default);
    Task<Result> ToggleUserStatusAsync(long userId, bool isActive, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AdminConsultationDto>>> GetAllConsultationsAsync(string? status = null, int limit = 100, CancellationToken ct = default);
}
