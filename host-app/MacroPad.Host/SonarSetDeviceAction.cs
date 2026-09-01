using System.Net.Http;

namespace MacroPad.Host;

/// <summary>
/// Change le périphérique physique de sortie ("render") ou d'entrée ("mic") dans Sonar.
/// Contrairement aux canaux de mixage (game/chat/media/aux), il n'y a qu'une seule
/// sortie physique et une seule entrée physique en mode classic — pas besoin de
/// boucler sur plusieurs canaux.
/// </summary>
public class SonarSetDeviceAction : IAction
{
    private static readonly HttpClient Client = CreateClient();

    private readonly string _address;
    private readonly string _deviceId;
    private readonly string _channel; // "render" ou "mic"

    public SonarSetDeviceAction(string address, string channel, string deviceId)
    {
        _address = address;
        _channel = channel;
        _deviceId = deviceId;
    }

    public void Execute() => _ = ApplyAsync();

    private async Task ApplyAsync()
    {
        var encodedId = Uri.EscapeDataString(_deviceId);
        var url = $"http://{_address}/classicRedirections/{_channel}/deviceId/{encodedId}";

        try
        {
            var response = await Client.PutAsync(url, new StringContent(""));
            Console.WriteLine(response.IsSuccessStatusCode
                ? $"[SonarDevice] {_channel} -> {_deviceId} OK"
                : $"[SonarDevice] {_channel} -> ERREUR ({response.StatusCode}) : {url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SonarDevice] EXCEPTION ({_channel}) : {ex.Message}");
        }
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        return new HttpClient(handler);
    }
}