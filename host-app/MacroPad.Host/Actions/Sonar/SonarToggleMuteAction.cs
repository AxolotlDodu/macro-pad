using System.Net.Http;
using System.Text.Json;

namespace MacroPad.Host;

public class SonarToggleMuteAction : IAction
{
    private static readonly HttpClient Client = CreateClient();

    private readonly string _address;
    private readonly string _channel;
    private bool? _lastKnownMuted;

    private readonly IPadNotifier? _notifier;

    public SonarToggleMuteAction(string address, string channel, IPadNotifier? notifier = null)
    {
        _address = address;
        _channel = channel;
        _notifier = notifier;
    }

    public void Execute() => _ = ToggleAsync();

    private async Task ToggleAsync()
    {
        try
        {
            var desiredMuted = !(_lastKnownMuted ?? false);
            var streamerMode = await SonarStreamerMode.IsActiveAsync(Client, _address);
            bool actualMuted = desiredMuted;

            if (!streamerMode)
            {
                var url = $"http://{_address}/volumeSettings/classic/{_channel}/Mute/{(desiredMuted ? "true" : "false")}";
                var response = await Client.PutAsync(url, new StringContent(""));
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[SonarToggleMute] ERREUR ({response.StatusCode}) : {url}");
                    return; // on ne fige pas l'état si la requête a échoué
                }
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                actualMuted = doc.RootElement.GetProperty("devices").GetProperty(_channel)
                    .GetProperty("classic").GetProperty("muted").GetBoolean();
            }
            else
            {
                bool allOk = true;

                // Game/Chat/Media/Aux : uniquement monitoring (sortie PC).
                // Micro (chatCapture) : monitoring ET streaming.
                foreach (var slider in SonarStreamerMode.VolumeSlidersFor(_channel))
                {
                    var url = $"http://{_address}/volumeSettings/streamer/{slider}/{_channel}/isMuted/{(desiredMuted ? "true" : "false")}";
                    var response = await Client.PutAsync(url, new StringContent(""));

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[SonarToggleMute] {_channel}/{slider} -> muted={desiredMuted} OK");
                    }
                    else
                    {
                        allOk = false;
                        var body = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[SonarToggleMute] ERREUR ({response.StatusCode}) sur {url} : {body}");
                    }
                }

                if (!allOk) return; // état non figé -> le prochain appui retentera dans le même sens
            }

            _lastKnownMuted = actualMuted;
            _notifier?.ShowNotification($"{SonarChannels.GetDisplayName(_channel)}: {(actualMuted ? "Mute" : "Actif")}");
            Console.WriteLine($"[SonarToggleMute] {_channel} -> muted={actualMuted} (streamer={streamerMode})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SonarToggleMute] Erreur : {ex.Message}");
        }
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true };
        return new HttpClient(handler);
    }
}