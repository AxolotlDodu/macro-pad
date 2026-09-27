using System.Net.Http;
using System.Text.Json;

namespace MacroPad.Host;

public class SonarCycleDeviceAction : IAction
{
    private static readonly HttpClient Client = CreateClient();

    private readonly string _address;
    private readonly string _channel; // "render" ou "mic"
    private readonly List<string>? _explicitDeviceIds;
    private readonly List<string>? _excludedDevices; // id ou nom convivial (substring, insensible à la casse)

    private (string Id, string FriendlyName)[] _devices = Array.Empty<(string, string)>();
    private int _index = -1;
    private bool _initialized;

    private readonly IPadNotifier? _notifier;

    public SonarCycleDeviceAction(string address, string channel, List<string>? explicitDeviceIds,
        List<string>? excludedDevices, IPadNotifier? notifier = null)
    {
        _address = address;
        _channel = channel;
        _explicitDeviceIds = explicitDeviceIds;
        _excludedDevices = excludedDevices;
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

        Console.WriteLine($"[SonarCycleDevice] {_channel} -> device #{_index} ({friendlyName})");
        new SonarSetDeviceAction(_address, _channel, deviceId, _notifier).Execute();
    }

    private async Task InitializeAsync()
    {
        var dataFlow = _channel == "mic" ? "capture" : "render";
        var resolved = await SonarDeviceResolver.GetPhysicalDevicesAsync(Client, _address, dataFlow);

        IEnumerable<(string Id, string FriendlyName)> list = _explicitDeviceIds is { Count: > 0 }
            ? _explicitDeviceIds.Select(id => (id, resolved.FirstOrDefault(d => d.Id == id).FriendlyName ?? id))
            : resolved;

        if (_excludedDevices is { Count: > 0 })
        {
            list = list.Where(d => !_excludedDevices.Any(excl =>
                d.Id.Contains(excl, StringComparison.OrdinalIgnoreCase) ||
                d.FriendlyName.Contains(excl, StringComparison.OrdinalIgnoreCase)));
        }

        _devices = list.ToArray();

        Console.WriteLine($"[SonarCycleDevice] {_channel} : {_devices.Length} périphérique(s) retenu(s) -> "
            + string.Join(", ", _devices.Select(d => d.FriendlyName)));

        await SyncIndexAsync();
    }

    private async Task SyncIndexAsync()
    {
        try
        {
            var streamerMode = await SonarStreamerMode.IsActiveAsync(Client, _address);
            var basePath = SonarStreamerMode.RedirectionBasePath(streamerMode);
            var redirectionChannel = _channel == "mic" ? "mic" : SonarStreamerMode.RenderRedirectionKey(streamerMode);

            var json = await Client.GetStringAsync($"http://{_address}/{basePath}");
            using var doc = JsonDocument.Parse(json);

            string? currentId = null;
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (entry.GetProperty("id").GetString() == redirectionChannel)
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
        var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true };
        return new HttpClient(handler);
    }
}