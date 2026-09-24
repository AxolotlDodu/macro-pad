using System.Net.Http;
using System.Text.Json;


namespace MacroPad.Host;

/// <summary>
/// L'API Sonar n'expose pas d'endpoint "volume relatif" : /Volume/{value} attend une valeur
/// absolue 0.00-1.00 (cf. reverse engineering github.com/wex/sonar-rev et
/// github.com/PrzemekkkYT/GGSonarRev). On maintient donc un cache local par canal, initialisé
/// via un GET /volumeSettings/classic la première fois, puis mis à jour localement à chaque
/// PUT réussi — pas de round-trip GET à chaque cran d'encodeur.
/// </summary>
internal static class SonarVolumeState
{
    private static readonly Dictionary<string, double> Cache = new();
    private static readonly object Lock = new();

    public static async Task<double> GetOrFetchAsync(HttpClient client, string address, string channel)
    {
        lock (Lock)
        {
            if (Cache.TryGetValue(channel, out var cached)) return cached;
        }

        double volume = 0.5;
        try
        {
            var json = await client.GetStringAsync($"http://{address}/volumeSettings/classic");
            using var doc = JsonDocument.Parse(json);
            volume = doc.RootElement
                .GetProperty("devices")
                .GetProperty(channel)
                .GetProperty("classic")
                .GetProperty("volume")
                .GetDouble();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SonarVolume] Lecture initiale du volume impossible pour \"{channel}\" (défaut 0.5) : {ex.Message}");
        }

        lock (Lock) { Cache[channel] = volume; }
        return volume;
    }

    public static void Set(string channel, double volume)
    {
        lock (Lock) { Cache[channel] = volume; }
    }
}
