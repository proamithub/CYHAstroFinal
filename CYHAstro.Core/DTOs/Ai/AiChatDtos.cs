namespace CYHAstro.Core.DTOs.Ai;

public sealed class AiChatMessageDto
{
    public string Role { get; set; } = "user"; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class AiChatSessionDto
{
    public long Id { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public string Title { get; set; } = "Astrology Guidance";
    public string Topic { get; set; } = "General";
    public long? BirthProfileId { get; set; }
    public string? BirthProfileName { get; set; }
    public int TokensUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<AiChatMessageDto> Messages { get; set; } = new();
}

public sealed class SendAiChatMessageRequestDto
{
    public string? SessionPublicId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Topic { get; set; } = "General";
    public long? BirthProfileId { get; set; }
    public string Language { get; set; } = "en";
}

public sealed class SendAiChatMessageResponseDto
{
    public string SessionPublicId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public AiChatMessageDto Reply { get; set; } = new();
}
