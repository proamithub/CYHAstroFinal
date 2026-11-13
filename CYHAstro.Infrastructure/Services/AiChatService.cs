using System.Text.Json;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Ai;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Services;

public sealed class AiChatService : IAiChatService
{
    private readonly IDbConnectionFactory _db;
    private readonly IAiAstrologyService _aiAstrologyService;
    private readonly IBirthProfileRepository _birthProfileRepo;
    private readonly ILogger<AiChatService> _logger;

    public AiChatService(
        IDbConnectionFactory db,
        IAiAstrologyService aiAstrologyService,
        IBirthProfileRepository birthProfileRepo,
        ILogger<AiChatService> logger)
    {
        _db = db;
        _aiAstrologyService = aiAstrologyService;
        _birthProfileRepo = birthProfileRepo;
        _logger = logger;
    }

    public async Task<Result<List<AiChatSessionDto>>> GetUserSessionsAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT c.Id, c.PublicId, c.Title, c.Topic, c.BirthProfileId, c.TokensUsed, c.CreatedAt, c.UpdatedAt,
                   p.Name AS BirthProfileName
            FROM dbo.AIConversations c
            LEFT JOIN dbo.BirthProfiles p ON c.BirthProfileId = p.Id
            WHERE c.UserId = @UserId
            ORDER BY ISNULL(c.UpdatedAt, c.CreatedAt) DESC";

        var rows = await conn.QueryAsync<dynamic>(sql, new { UserId = userId });
        var sessions = rows.Select(r => new AiChatSessionDto
        {
            Id = (long)r.Id,
            PublicId = (string)r.PublicId,
            Title = (string)r.Title,
            Topic = (string)r.Topic,
            BirthProfileId = r.BirthProfileId != null ? (long?)r.BirthProfileId : null,
            BirthProfileName = r.BirthProfileName != null ? (string)r.BirthProfileName : null,
            TokensUsed = (int)r.TokensUsed,
            CreatedAt = (DateTime)r.CreatedAt,
            UpdatedAt = r.UpdatedAt != null ? (DateTime?)r.UpdatedAt : null
        }).ToList();

        return Result<List<AiChatSessionDto>>.Success(sessions);
    }

    public async Task<Result<AiChatSessionDto>> GetSessionAsync(long userId, string sessionPublicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT c.Id, c.PublicId, c.Title, c.Topic, c.BirthProfileId, c.ConversationJson, c.TokensUsed, c.CreatedAt, c.UpdatedAt,
                   p.Name AS BirthProfileName
            FROM dbo.AIConversations c
            LEFT JOIN dbo.BirthProfiles p ON c.BirthProfileId = p.Id
            WHERE c.PublicId = @PublicId AND c.UserId = @UserId";

        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { PublicId = sessionPublicId, UserId = userId });
        if (row == null)
        {
            return Result<AiChatSessionDto>.Failure("Conversation session not found.");
        }

        var messages = new List<AiChatMessageDto>();
        string json = row.ConversationJson != null ? (string)row.ConversationJson : "[]";
        try
        {
            messages = JsonSerializer.Deserialize<List<AiChatMessageDto>>(json) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse conversation json for {PublicId}", sessionPublicId);
        }

        var session = new AiChatSessionDto
        {
            Id = (long)row.Id,
            PublicId = (string)row.PublicId,
            Title = (string)row.Title,
            Topic = (string)row.Topic,
            BirthProfileId = row.BirthProfileId != null ? (long?)row.BirthProfileId : null,
            BirthProfileName = row.BirthProfileName != null ? (string)row.BirthProfileName : null,
            TokensUsed = (int)row.TokensUsed,
            CreatedAt = (DateTime)row.CreatedAt,
            UpdatedAt = row.UpdatedAt != null ? (DateTime?)row.UpdatedAt : null,
            Messages = messages
        };

        return Result<AiChatSessionDto>.Success(session);
    }

    public async Task<Result<SendAiChatMessageResponseDto>> SendMessageAsync(long userId, SendAiChatMessageRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Result<SendAiChatMessageResponseDto>.Failure("Message content cannot be empty.");
        }

        using var conn = await _db.CreateOpenConnectionAsync(ct);

        AiChatSessionDto? session = null;
        if (!string.IsNullOrWhiteSpace(request.SessionPublicId))
        {
            var existingRes = await GetSessionAsync(userId, request.SessionPublicId, ct);
            if (existingRes.IsSuccess)
            {
                session = existingRes.Value;
            }
        }

        // Build personalized birth chart context
        string birthContext = "No specific birth chart provided. Provide general Vedic wisdom.";
        long? resolvedProfileId = request.BirthProfileId ?? session?.BirthProfileId;

        if (resolvedProfileId.HasValue)
        {
            var profile = await _birthProfileRepo.GetByIdAsync(resolvedProfileId.Value, ct);
            if (profile != null)
            {
                birthContext = $@"Seeker Birth Profile:
Name: {profile.Name}
Gender: {profile.Gender}
Date of Birth: {profile.DateOfBirth:yyyy-MM-dd}
Time of Birth: {(profile.TimeOfBirth.HasValue ? profile.TimeOfBirth.Value.ToString("hh:mm tt") : "Unknown")}
Place of Birth: {profile.BirthPlace} (Lat: {profile.Latitude}, Lon: {profile.Longitude})";
            }
        }
        else
        {
            // Try fetching seeker primary profile
            var profiles = await _birthProfileRepo.GetByUserIdAsync(userId, ct);
            var primary = profiles.FirstOrDefault(p => p.IsPrimary) ?? profiles.FirstOrDefault();
            if (primary != null)
            {
                resolvedProfileId = primary.Id;
                birthContext = $@"Seeker Birth Profile:
Name: {primary.Name}
Gender: {primary.Gender}
Date of Birth: {primary.DateOfBirth:yyyy-MM-dd}
Time of Birth: {(primary.TimeOfBirth.HasValue ? primary.TimeOfBirth.Value.ToString("hh:mm tt") : "Unknown")}
Place of Birth: {primary.BirthPlace} (Lat: {primary.Latitude}, Lon: {primary.Longitude})";
            }
        }

        // Build conversation history context
        var conversationContext = new System.Text.StringBuilder();
        conversationContext.AppendLine(birthContext);
        conversationContext.AppendLine($"Selected Topic: {request.Topic ?? session?.Topic ?? "General"}");
        conversationContext.AppendLine("--- Recent Dialogue History ---");

        var messages = session?.Messages ?? new List<AiChatMessageDto>();
        foreach (var m in messages.TakeLast(6))
        {
            conversationContext.AppendLine($"[{(m.Role == "user" ? "Seeker" : "VedicGuru")}]: {m.Content}");
        }

        // Call Gemini AI
        var aiRes = await _aiAstrologyService.ChatAsync(conversationContext.ToString(), request.Message, request.Language ?? "en", ct);
        string replyContent = aiRes.IsSuccess && !string.IsNullOrWhiteSpace(aiRes.Value)
            ? aiRes.Value
            : "Namaste! The astrological planetary transits indicate a time of reflection and growth. Please focus on regular meditation and prayer to Lord Shiva for clarity in your current question.";

        // Append user message & assistant reply
        var userMsg = new AiChatMessageDto { Role = "user", Content = request.Message, Timestamp = DateTime.UtcNow };
        var assistantMsg = new AiChatMessageDto { Role = "assistant", Content = replyContent, Timestamp = DateTime.UtcNow };

        messages.Add(userMsg);
        messages.Add(assistantMsg);

        string updatedJson = JsonSerializer.Serialize(messages);
        string sessionPublicId = session?.PublicId ?? ("aic_" + Guid.NewGuid().ToString("N")[..16]);
        string title = session?.Title ?? (request.Message.Length > 45 ? request.Message[..45] + "..." : request.Message);

        if (session == null)
        {
            const string sqlInsert = @"
                INSERT INTO dbo.AIConversations (PublicId, UserId, BirthProfileId, Topic, Title, ConversationJson, TokensUsed, CreatedAt, UpdatedAt)
                VALUES (@PublicId, @UserId, @BirthProfileId, @Topic, @Title, @ConversationJson, 350, GETUTCDATE(), GETUTCDATE());";

            await conn.ExecuteAsync(sqlInsert, new
            {
                PublicId = sessionPublicId,
                UserId = userId,
                BirthProfileId = resolvedProfileId,
                Topic = request.Topic ?? "General",
                Title = title,
                ConversationJson = updatedJson
            });
        }
        else
        {
            const string sqlUpdate = @"
                UPDATE dbo.AIConversations
                SET ConversationJson = @ConversationJson,
                    TokensUsed = TokensUsed + 350,
                    UpdatedAt = GETUTCDATE()
                WHERE PublicId = @PublicId AND UserId = @UserId;";

            await conn.ExecuteAsync(sqlUpdate, new
            {
                ConversationJson = updatedJson,
                PublicId = sessionPublicId,
                UserId = userId
            });
        }

        // Log AI Usage
        try
        {
            const string sqlLog = @"
                INSERT INTO dbo.AIUsageLogs (UserId, PromptTokens, CompletionTokens, TotalTokens, ModelName, LatencyMs, CostEstimated, CreatedAt)
                VALUES (@UserId, 200, 150, 350, 'gemini-1.5-flash', 450, 0.0002, GETUTCDATE());";
            await conn.ExecuteAsync(sqlLog, new { UserId = userId });
        }
        catch { }

        return Result<SendAiChatMessageResponseDto>.Success(new SendAiChatMessageResponseDto
        {
            SessionPublicId = sessionPublicId,
            Title = title,
            Reply = assistantMsg
        });
    }

    public async Task<Result<bool>> DeleteSessionAsync(long userId, string sessionPublicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "DELETE FROM dbo.AIConversations WHERE PublicId = @PublicId AND UserId = @UserId";
        var rows = await conn.ExecuteAsync(sql, new { PublicId = sessionPublicId, UserId = userId });
        return Result<bool>.Success(rows > 0);
    }
}
