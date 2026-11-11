namespace CYHAstro.Core.Configuration;

public sealed class LiveKitSettings
{
    public const string SectionName = "LiveKitSettings";

    public string ApiKey { get; set; } = "devkey";
    public string ApiSecret { get; set; } = "secret123456789012345678901234567890";
    public string LiveKitUrl { get; set; } = "wss://livekit.cyhastro.com";
}
