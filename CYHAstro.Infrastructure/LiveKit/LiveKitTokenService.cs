using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CYHAstro.Core.Configuration;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CYHAstro.Infrastructure.LiveKit;

public sealed class LiveKitTokenService : ILiveKitTokenService
{
    private readonly LiveKitSettings _settings;

    public LiveKitTokenService(IOptions<LiveKitSettings> options)
    {
        _settings = options.Value;
    }

    public string GenerateAccessToken(string roomName, string participantIdentity, string participantName, bool canPublish = true, bool canSubscribe = true)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.ApiSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var videoGrants = new Dictionary<string, object>
        {
            ["room"] = roomName,
            ["roomJoin"] = true,
            ["canPublish"] = canPublish,
            ["canSubscribe"] = canSubscribe,
            ["canPublishData"] = true
        };

        var claims = new List<Claim>
        {
            new("sub", participantIdentity),
            new("name", participantName),
            new("video", JsonSerializer.Serialize(videoGrants), JsonClaimValueTypes.Json)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.ApiKey,
            claims: claims,
            notBefore: DateTime.UtcNow.AddSeconds(-10),
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
