namespace CYHAstro.Core.DTOs.User;

public sealed record NotificationDto
{
    public long Id { get; init; }
    public long UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public string Type { get; init; } = "SYSTEM"; // SYSTEM, CONSULTATION, WALLET, PROMOTION
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}
