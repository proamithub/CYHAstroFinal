using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Api.Controllers.v1;

/// <summary>
/// Health check endpoint. No authentication required.
/// Used by load balancers, monitoring, and CI/CD pipelines.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>GET /api/v1/health — Returns API health status.</summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "CYHAstro.Api",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }
}
