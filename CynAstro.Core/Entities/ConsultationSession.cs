namespace CynAstro.Core.Entities;

/// <summary>
/// Consultation session lifecycle entity.
/// Session goes through: Requested → Accepted → Active → Ended → Billed
/// </summary>
public sealed class ConsultationSession : BaseEntity
{
    public string PublicId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public long AstrologerId { get; set; }
    public string SessionType { get; set; } = string.Empty; // CHAT, AUDIO, VIDEO
    public string Status { get; set; } = "REQUESTED";
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ConnectedAt { get; set; }   // Server-verified connection time
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }     // Server-calculated, NOT client
    public decimal RatePerMinute { get; set; }
    public decimal TotalCharge { get; set; }
    public string? LiveKitRoomName { get; set; }
    public string? EndReason { get; set; }       // USER_END, ASTROLOGER_END, TIMEOUT, ERROR
    public bool IsBilled { get; set; }
    public string? IdempotencyKey { get; set; }
}

public static class SessionStatus
{
    public const string Requested = "REQUESTED";
    public const string Accepted = "ACCEPTED";
    public const string Active = "ACTIVE";
    public const string Ended = "ENDED";
    public const string Billed = "BILLED";
    public const string Cancelled = "CANCELLED";
    public const string Failed = "FAILED";
}
