namespace CYHAstro.Core.Interfaces.Services;

public interface ILiveKitTokenService
{
    string GenerateAccessToken(string roomName, string participantIdentity, string participantName, bool canPublish = true, bool canSubscribe = true);
}
