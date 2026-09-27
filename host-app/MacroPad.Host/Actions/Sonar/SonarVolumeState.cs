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

        if (slider is null) // mode classique : lecture initiale possible
        {
            try
            {
                var json = await client.GetStringAsync($"http://{address}/volumeSettings/classic");
                using var doc = JsonDocument.Parse(json);
                volume = doc.RootElement
                    .GetProperty("devices").GetProperty(channel)
                    .GetProperty("classic").GetProperty("volume").GetDouble();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SonarVolume] Lecture initiale impossible pour \"{channel}\" (défaut 0.5) : {ex.Message}");
            }
        }
        // Mode streamer : pas de lecture initiale implémentée (structure JSON non confirmée),
        // on part de 0.5 puis le cache se resynchronise après le premier réglage.

        lock (Lock) { Cache[key] = volume; }
        return volume;
    }

    public static void Set(string channel, double volume, string? slider = null)
    {
        lock (Lock) { Cache[CacheKey(channel, slider)] = volume; }
    }

    private static string CacheKey(string channel, string? slider) => slider is null ? channel : $"{channel}:{slider}";
}