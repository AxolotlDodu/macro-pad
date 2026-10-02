using System.Net.Http;
using System.Text.Json;

namespace MacroPad.Host;

/// <summary>Cache local de volume par canal (+ slider en mode streamer, cf. SonarStreamerMode).</summary>
internal static class SonarVolumeState
{
    private static readonly Dictionary<string, double> Cache = new();
    private static readonly object Lock = new();

    public static async Task<double> GetOrFetchAsync(HttpClient client, string address, string channel, string? slider = null)
    {
        var key = CacheKey(channel, slider);
        lock (Lock)
        {
            if (Cache.TryGetValue(key, out var cached)) return cached;
        }

        double volume = 0.5;

                try
        {
            if (slider is null)
            {
                var json = await client.GetStringAsync($"http://{address}/volumeSettings/classic");
                using var doc = JsonDocument.Parse(json);
                volume = doc.RootElement
                    .GetProperty("devices").GetProperty(channel)
                    .GetProperty("classic").GetProperty("volume").GetDouble();
            }
            else
            {
                var json = await client.GetStringAsync($"http://{address}/volumeSettings/streamer");
                using var doc = JsonDocument.Parse(json);
                var sliderKey = slider == "streaming" ? "stream" : "monitoring";

                JsonElement node;
                if (!doc.RootElement.GetProperty("devices").TryGetProperty(channel, out node))
                    node = doc.RootElement.GetProperty("masters"); // master en streamer

                if (!node.TryGetProperty(sliderKey, out var sliderNode) && !node.TryGetProperty(slider, out sliderNode))
                    throw new KeyNotFoundException($"slider \"{slider}\" introuvable");

                volume = sliderNode.GetProperty("volume").GetDouble();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SonarVolume] Lecture initiale impossible pour \"{channel}\"{(slider is null ? "" : $"/{slider}")} (défaut 0.5) : {ex.Message}");
        }

        lock (Lock) { Cache[key] = volume; }
        return volume;
    }

    public static void Set(string channel, double volume, string? slider = null)
    {
        lock (Lock) { Cache[CacheKey(channel, slider)] = volume; }
    }

    private static string CacheKey(string channel, string? slider) => slider is null ? channel : $"{channel}:{slider}";
}