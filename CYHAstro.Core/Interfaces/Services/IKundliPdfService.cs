using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Astrology;

namespace CYHAstro.Core.Interfaces.Services;

public interface IKundliPdfService
{
    Task<Result<byte[]>> GenerateKundliPdfAsync(KundliChartDto chart, KundliRequestDto request, CancellationToken ct = default);
}
