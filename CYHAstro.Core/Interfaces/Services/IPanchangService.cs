using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Panchang;

namespace CYHAstro.Core.Interfaces.Services;

public interface IPanchangService
{
    Task<Result<PanchangResponseDto>> GetPanchangAsync(PanchangRequestDto request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ChoghadiyaPeriodDto>>> GetChoghadiyaAsync(DateTime date, decimal lat, decimal lon, decimal tzOffset, CancellationToken ct = default);
}
