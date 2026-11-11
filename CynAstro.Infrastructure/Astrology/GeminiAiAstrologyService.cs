using CynAstro.Core.Common;
using CynAstro.Core.Configuration;
using CynAstro.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace CynAstro.Infrastructure.Astrology;

/// <summary>
/// Google Gemini REST API implementation for AI Astrology.
/// Strict prompt discipline: Interprets verified calculated Vedic data only.
/// </summary>
public sealed class GeminiAiAstrologyService : IAiAstrologyService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiAiAstrologyService> _logger;

    public GeminiAiAstrologyService(
        HttpClient httpClient,
        IOptions<GeminiSettings> options,
        ILogger<GeminiAiAstrologyService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<Result<string>> ExplainKundliAsync(
        string chartSummaryJson,
        string userQuestion,
        string language = "en",
        CancellationToken ct = default)
    {
        var systemInstruction = $@"You are a knowledgeable Vedic Astrologer for the CynAstro platform.
CRITICAL RULES:
1. You MUST interpret ONLY the provided deterministic astronomical/Vedic calculation data.
2. DO NOT invent, assume, or alter planetary positions, signs, houses, or nakshatras.
3. Respond in the requested language: {language}.
4. Provide positive, constructive, and culturally respectful guidance.
5. Emphasize remedies (mantras, charity, mindfulness) rather than fear-mongering.";

        var prompt = $@"Verified Kundli Chart Data:
{chartSummaryJson}

User Query:
{userQuestion}

Please provide a clear, accurate Vedic astrological analysis explaining the houses and planetary influences.";

        return await CallGeminiAsync(systemInstruction, prompt, ct);
    }

    public async Task<Result<string>> ChatAsync(
        string systemContext,
        string userMessage,
        string language = "en",
        CancellationToken ct = default)
    {
        var systemInstruction = $@"You are an AI Vedic Astrologer on CynAstro.
Rules:
- Be warm, empathetic, and grounded in Vedic principles.
- Use the provided user context and birth profile data strictly.
- Reply in language: {language}.
- Give practical remedies and spiritual uplifting advice.";

        var prompt = $@"Context:
{systemContext}

User Message:
{userMessage}";

        return await CallGeminiAsync(systemInstruction, prompt, ct);
    }

    private async Task<Result<string>> CallGeminiAsync(
        string systemInstruction,
        string prompt,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return Result<string>.Failure("Gemini API key is not configured.");
        }

        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.ModelName}:generateContent?key={_settings.ApiKey}";

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.4,
                    topK = 32,
                    topP = 0.9,
                    maxOutputTokens = 1024
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API returned error {StatusCode}: {Error}", response.StatusCode, errorText);
                return Result<string>.Failure($"Gemini API error ({response.StatusCode}): {errorText}");
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var text))
            {
                var resultText = text.GetString();
                return Result<string>.Success(resultText ?? string.Empty);
            }

            return Result<string>.Failure("Gemini API returned an empty or unparseable response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Gemini API");
            return Result<string>.Failure(ex.Message);
        }
    }
}
