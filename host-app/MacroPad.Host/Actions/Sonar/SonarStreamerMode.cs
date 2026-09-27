using System.Net.Http;

namespace MacroPad.Host;

/// <summary>
/// Endpoints Sonar en mode streamer, confirmés par github.com/wex/sonar-rev
/// (volumeSettings/classic) et son extension streamer mode (sliders "streaming"/"monitoring") :
/// GET /mode -> "classic" ou "stream" ; classique : /volumeSettings/classic/{channel}/... ;
/// streamer : /volumeSettings/streamer/{slider}/{channel}/Volume|Mute/....
/// </summary>
public static class SonarStreamerMode
{
    public const string Streaming = "streaming";
    public const string Monitoring = "monitoring";

    public static async Task<bool> IsActiveAsync(HttpClient client, string address)
    {
        try
        {
            var raw = (await client.GetStringAsync($"http://{address}/mode")).Trim().Trim('"');
            return string.Equals(raw, "stream", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false; // Sonar/endpoint indisponible -> on suppose le mode classique
        }
    }

    /// <summary>Sliders à appliquer pour un canal donné en mode streamer.
    /// Micro (chatCapture) : synchronisé sur les deux sliders. Game/Chat/Media/Aux :
    /// uniquement la sortie PC (monitoring), jamais le flux envoyé au stream (streaming).</summary>
    public static string[] VolumeSlidersFor(string channel) =>
        channel == "chatCapture" ? new[] { Streaming, Monitoring } : new[] { Monitoring };

    public static string RedirectionBasePath(bool streamerMode) => streamerMode ? "streamRedirections" : "classicRedirections";

    /// <summary>Clé de canal pour la redirection de sortie physique ("render" en classique,
    /// "monitoring" en streamer — la sortie "streaming" reste dédiée au flux, jamais touchée).</summary>
    public static string RenderRedirectionKey(bool streamerMode) => streamerMode ? "monitoring" : "render";
}