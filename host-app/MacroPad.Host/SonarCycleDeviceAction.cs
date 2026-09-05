using System.Net.Http;
using System.Text.Json;

namespace MacroPad.Host;

public class SonarCycleDeviceAction : IAction
{
    private static readonly HttpClient Client = CreateClient();

    private readonly string _address;
    private readonly string _channel; // "render" ou "mic"
    private readonly List<string>? _explicitDeviceIds;

    private (string Id, string FriendlyName)[] _devices = Array.Empty<(string, string)>();
    private int _index = -1;
    private bool _initialized;

    private readonly IPadNotifier? _notifier;

    public SonarCycleDeviceAction(string address, string channel, List<string>? explicitDeviceIds, IPadNotifier? notifier = null)
    {
        _address = address;
        _channel = channel;
        _explicitDeviceIds = explicitDeviceIds;
        _notifier = notifier;
    }

    public void Execute() => _ = ExecuteAsync();

    private async Task ExecuteAsync()
    {
        if (!_initialized)
        {
            await InitializeAsync();
            _initialized = true;
        }

        if (_devices.Length == 0)
        {
            Console.WriteLine($"[SonarCycleDevice] Aucun périphérique disponible pour \"{_channel}\".");
            return;
        }

        _index = (_index + 1) % _devices.Length;
        var (deviceId, friendlyName) = _devices[_index];

        Console.WriteLine($"[SonarCycleDevice] {_channel} -> device #{_index} ({deviceId})");
        new SonarSetDeviceAction(_address, _channel, deviceId).Execute();

        var label = _channel == "mic" ? "Micro" : "Sortie";
        _notifier?.ShowNotification($"{label}: {friendlyName}");
    }

    private async Task InitializeAsync()
    {
        var dataFlow = _channel == "mic" ? "capture" : "render";

        if (_explicitDeviceIds is { Count: > 0 })
        {
            // On résout quand même les noms conviviaux pour les IDs explicites, pour l'affichage.
            var resolved = await SonarDeviceResolver.GetPhysicalDevicesAsync(Client, _address, dataFlow);
            _devices = _explicitDeviceIds
                .Select(id => (id, resolved.FirstOrDefault(d => d.Id == id).FriendlyName ?? id))
                .ToArray();
        }
        else
        {
            var devices = await SonarDeviceResolver.GetPhysicalDevicesAsync(Client, _address, dataFlow);
            _devices = devices.Select(d => (d.Id, d.FriendlyName)).ToArray();

            Console.WriteLine($"[SonarCycleDevice] {_channel} : {devices.Count} périphérique(s) détecté(s) -> "
                + string.Join(", ", devices.Select(d => d.FriendlyName)));
        }

        await SyncIndexAsync();
    }

    private async Task SyncIndexAsync()
    {
        try
        {
            var json = await Client.GetStringAsync($"http://{_address}/classicRedirections");
            using var doc = JsonDocument.Parse(json);

            string? currentId = null;
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (entry.GetProperty("id").GetString() == _channel)
                {
                    currentId = entry.GetProperty("deviceId").GetString();
                    break;
                }
            }

            if (currentId is not null)
            {
                var idx = Array.FindIndex(_devices, d => d.Id == currentId);
                if (idx >= 0) _index = idx;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SonarCycleDevice] Sync initiale échouée ({_channel}) : {ex.Message}");
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