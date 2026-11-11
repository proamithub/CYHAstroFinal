using CynAstro.Core.Common;

namespace CynAstro.Core.Interfaces.Services;

/// <summary>
/// AI Astrology Service powered by Google Gemini.
/// Explains calculated deterministic astrology data without hallucinating positions.
/// </summary>
public interface IAiAstrologyService
{
    /// <summary>
    /// Explain calculated Kundli chart data.
    /// </summary>
    Task<Result<string>> ExplainKundliAsync(
        string chartSummaryJson,
        string userQuestion,
        string language = "en",
        CancellationToken ct = default);

    /// <summary>
    /// Provide conversational AI astrology advice grounded in verified birth chart.
    /// </summary>
    Task<Result<string>> ChatAsync(
        string systemContext,
        string userMessage,
        string language = "en",
        CancellationToken ct = default);
}
