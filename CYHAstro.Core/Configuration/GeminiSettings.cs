namespace CYHAstro.Core.Configuration;

/// <summary>
/// Configuration settings for Google Gemini Generative AI.
/// Used for AI Astrology consultations and report explanations.
/// </summary>
public sealed class GeminiSettings
{
    public const string SectionName = "GeminiSettings";

    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gemini-1.5-flash";
}
