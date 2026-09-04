namespace MacroPad.Host;

/// <summary>Liste fermée des channels Sonar utilisés par sonar-toggle-mute et sonar-volume.
/// À ajuster si tu ajoutes/retires des channels dans Sonar.</summary>
public static class SonarChannels
{
    public static readonly string[] All = { "master", "game", "chatRender", "media", "aux", "chatCapture" };
}