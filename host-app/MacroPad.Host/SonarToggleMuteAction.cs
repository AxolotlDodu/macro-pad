using System.Net.Http;
using System.Text.Json;

namespace MacroPad.Host;

public class SonarToggleMuteAction : IAction
{
    private static readonly HttpClient Client = CreateClient();

    private readonly string _address;
    private readonly string _channel;
    private bool? _lastKnownMuted;

    public SonarToggleMuteAction(string address, string channel)
    {
        _address = address;
        _channel = channel;
    }

    public void Execute()
    {
        _ = ToggleAsync();
    }

    private async Task ToggleAsync()
    {
        try
        {
            // Première pression : on part du principe qu'on est démuté, donc on mute.
            // Ensuite, on se base sur l'état réel renvoyé par Sonar à chaque appel.
            var desiredMuted = !(_lastKnownMuted ?? false);

            var url = $"http://{_address}/volumeSettings/classic/{_channel}/Mute/{(desiredMuted ? "true" : "false")}";
            var response = await Client.PutAsync(url, new StringContent(""));
            var body = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            var actualMuted = doc.RootElement
                .GetProperty("devices")
                .GetProperty(_channel)
                .GetProperty("classic")
                .GetProperty("muted")
                .GetBoolean();

            _lastKnownMuted = actualMuted;
            Console.WriteLine($"[SonarToggleMute] {_channel} -> muted={actualMuted}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SonarToggleMute] Erreur : {ex.Message}");
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