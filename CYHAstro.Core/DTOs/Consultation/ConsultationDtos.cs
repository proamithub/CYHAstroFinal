namespace CYHAstro.Core.DTOs.Consultation;

public sealed record RequestConsultationDto
{
    public string AstrologerPublicId { get; init; } = string.Empty;
    public string SessionType { get; init; } = "CHAT"; // CHAT, AUDIO, VIDEO
}

public sealed record ConsultationResponseDto
{
    public long SessionId { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public long UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public long AstrologerId { get; init; }
    public string AstrologerName { get; init; } = string.Empty;
    public string? AstrologerPhoto { get; init; }
    public string SessionType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal RatePerMinute { get; init; }
    public string? LiveKitRoomName { get; init; }
    public string? LiveKitToken { get; init; }
    public DateTime? ConnectedAt { get; init; }
    public int DurationSeconds { get; init; }
    public decimal TotalCharge { get; init; }
    public decimal AstrologerEarnings { get; init; }
    public decimal PlatformCommission { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record EndConsultationDto
{
    public string SessionPublicId { get; init; } = string.Empty;
    public string? Reason { get; init; }
}

public sealed record ConsultationCompletedDto
{
    public string SessionPublicId { get; init; } = string.Empty;
    public int DurationSeconds { get; init; }
    public decimal TotalCharge { get; init; }
    public decimal UserRemainingBalance { get; init; }
    public decimal AstrologerEarnings { get; init; }
    public decimal PlatformCommission { get; init; }
    public string Status { get; init; } = "ENDED";
}

public sealed record ConsultationMessageDto
{
    public long Id { get; init; }
    public string SessionPublicId { get; init; } = string.Empty;
    public string SenderType { get; init; } = string.Empty; // USER, ASTROLOGER, SYSTEM
    public string SenderName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }
}
