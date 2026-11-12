using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Panchang;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Cache;
using CYHAstro.Infrastructure.Panchang;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Services;

public sealed class PanchangService : IPanchangService
{
    private readonly ICacheService _cache;
    private readonly ILogger<PanchangService> _logger;

    public PanchangService(ICacheService cache, ILogger<PanchangService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<PanchangResponseDto>> GetPanchangAsync(PanchangRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"panchang:{request.Date:yyyyMMdd}:{request.Latitude:F2}:{request.Longitude:F2}";
            var cached = await _cache.GetAsync<PanchangResponseDto>(cacheKey, ct);
            if (cached != null)
            {
                return Result<PanchangResponseDto>.Success(cached);
            }

            var result = VedicPanchangCalculator.Calculate(request);
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromHours(12), ct);

            return Result<PanchangResponseDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating Panchang for Date {Date}, Lat {Lat}, Lon {Lon}",
                request.Date, request.Latitude, request.Longitude);
            return Result<PanchangResponseDto>.Failure($"Failed to calculate Panchang: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<ChoghadiyaPeriodDto>>> GetChoghadiyaAsync(
        DateTime date, decimal lat, decimal lon, decimal tzOffset, CancellationToken ct = default)
    {
        var panchangReq = new PanchangRequestDto
        {
            Date = date,
            Latitude = lat,
            Longitude = lon,
            TimeZoneOffsetHours = tzOffset
        };

        var panchangRes = await GetPanchangAsync(panchangReq, ct);
        if (!panchangRes.IsSuccess || panchangRes.Value == null)
        {
            return Result<IReadOnlyList<ChoghadiyaPeriodDto>>.Failure(panchangRes.Error ?? "Could not calculate Choghadiya");
        }

        var all = panchangRes.Value.DayChoghadiya.Concat(panchangRes.Value.NightChoghadiya).ToList();
        return Result<IReadOnlyList<ChoghadiyaPeriodDto>>.Success(all.AsReadOnly());
    }
}
