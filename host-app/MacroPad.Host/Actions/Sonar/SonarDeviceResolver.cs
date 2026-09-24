using System.Net.Http;
using System.Text.Json;

namespace MacroPad.Host;

/// <summary>
/// Un device physique se reconnaît par role == "none" et isVad == false
/// (Sonar marque ses propres devices virtuels avec isVad == true).
/// </summary>
public static class SonarDeviceResolver
{
    public static async Task<List<(string Id, string FriendlyName)>> GetPhysicalDevicesAsync(
        HttpClient client, string address, string dataFlow)
    {
        var result = new List<(string, string)>();

        try
        {
            var json = await client.GetStringAsync($"http://{address}/audioDevices");
            using var doc = JsonDocument.Parse(json);

            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                var isPhysical = entry.GetProperty("role").GetString() == "none"
                    && !entry.GetProperty("isVad").GetBoolean()
                    && entry.GetProperty("dataFlow").GetString() == dataFlow
                    && entry.GetProperty("state").GetString() == "active";

                if (!isPhysical) continue;

                var id = entry.GetProperty("id").GetString();
                var name = entry.GetProperty("friendlyName").GetString();
                if (id is not null) result.Add((id, name ?? id));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SonarDeviceResolver] Échec récupération devices ({dataFlow}) : {ex.Message}");
        }

        return result;
    }
}