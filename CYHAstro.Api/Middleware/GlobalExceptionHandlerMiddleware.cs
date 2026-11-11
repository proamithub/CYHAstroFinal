using CYHAstro.Core.Exceptions;
using System.Net;
using System.Text.Json;

namespace CYHAstro.Api.Middleware;

/// <summary>
/// Global exception handler — converts exceptions to RFC 7807 Problem Details.
/// Never leaks stack traces to clients in production.
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        var (statusCode, title, detail) = ex switch
        {
            NotFoundException nfe      => (HttpStatusCode.NotFound, "Not Found", nfe.Message),
            ForbiddenException fe      => (HttpStatusCode.Forbidden, "Forbidden", fe.Message),
            ConcurrencyException ce    => (HttpStatusCode.Conflict, "Conflict", ce.Message),
            InsufficientBalanceException ibe => (HttpStatusCode.PaymentRequired, "Insufficient Balance", ibe.Message),
            OperationCanceledException => (HttpStatusCode.ServiceUnavailable, "Request Cancelled", "The request was cancelled."),
            _                          => (HttpStatusCode.InternalServerError, "Server Error", "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = _env.IsDevelopment() ? ex.Message : detail,
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}

public static class GlobalExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}
