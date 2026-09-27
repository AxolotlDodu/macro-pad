using System.Net.Http;

namespace MacroPad.Host;

public class SonarSetDeviceAction : IAction
{
    private static readonly HttpClient Client = CreateClient();

    private readonly string _address;
    private readonly string _deviceId;
    private readonly string _channel; // "render" ou "mic"

    private readonly IPadNotifier? _notifier;

    public SonarSetDeviceAction(string address, string channel, string deviceId, IPadNotifier? notifier = null)
    {
        _address = address;
        _channel = channel;
        _deviceId = deviceId;
        _notifier = notifier;
    }

    public void Execute() => _ = ApplyAsync();

    private async Task ApplyAsync()
    {
        var streamerMode = await SonarStreamerMode.IsActiveAsync(Client, _address);
        var basePath = SonarStreamerMode.RedirectionBasePath(streamerMode);
        // La sortie stream (mix "streaming") n'est jamais ciblée : uniquement la sortie PC.
        var redirectionChannel = _channel == "mic" ? "mic" : SonarStreamerMode.RenderRedirectionKey(streamerMode);

        var encodedId = Uri.EscapeDataString(_deviceId);
        var url = $"http://{_address}/{basePath}/{redirectionChannel}/deviceId/{encodedId}";

        try
        {
            var response = await Client.PutAsync(url, new StringContent(""));
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[SonarDevice] {redirectionChannel} -> {_deviceId} OK (streamer={streamerMode})");
                await NotifyAsync();
            }
            else
            {
                Console.WriteLine($"[SonarDevice] {redirectionChannel} -> ERREUR ({response.StatusCode}) : {url}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SonarDevice] EXCEPTION ({_channel}) : {ex.Message}");
        }
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true };
        return new HttpClient(handler);
    }

    private async Task NotifyAsync()
    {
        if (_notifier is null) return;

        var dataFlow = _channel == "mic" ? "capture" : "render";
        var devices = await SonarDeviceResolver.GetPhysicalDevicesAsync(Client, _address, dataFlow);
        var name = devices.FirstOrDefault(d => d.Id == _deviceId).FriendlyName ?? _deviceId;
        var label = _channel == "mic" ? "Micro" : "Sortie";

        _notifier.ShowNotification($"{label}: {name}");
    }
}