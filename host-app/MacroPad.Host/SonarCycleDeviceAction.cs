using System.Net.Http;
using System.Text.Json;

namespace MacroPad.Host;

public class SonarCycleDeviceAction : IAction
{
    private static readonly HttpClient Client = CreateClient();

    private readonly string _address;
    private readonly string _channel; // "render" ou "mic"
    private readonly List<string>? _explicitDeviceIds;

    private string[] _deviceIds = Array.Empty<string>();
    private int _index = -1;
    private bool _initialized;

    public SonarCycleDeviceAction(string address, string channel, List<string>? explicitDeviceIds)
    {
        _address = address;
        _channel = channel;
        _explicitDeviceIds = explicitDeviceIds;
    }

    public void Execute() => _ = ExecuteAsync();

    private async Task ExecuteAsync()
    {
        if (!_initialized)
        {
            await InitializeAsync();
            _initialized = true;
        }

        if (_deviceIds.Length == 0)
        {
            Console.WriteLine($"[SonarCycleDevice] Aucun périphérique disponible pour \"{_channel}\".");
            return;
        }

        _index = (_index + 1) % _deviceIds.Length;
        var deviceId = _deviceIds[_index];

        Console.WriteLine($"[SonarCycleDevice] {_channel} -> device #{_index} ({deviceId})");
        new SonarSetDeviceAction(_address, _channel, deviceId).Execute();
    }

    private async Task InitializeAsync()
    {
        if (_explicitDeviceIds is { Count: > 0 })
        {
            _deviceIds = _explicitDeviceIds.ToArray();
        }
        else
        {
            var dataFlow = _channel == "mic" ? "capture" : "render";
            var devices = await SonarDeviceResolver.GetPhysicalDevicesAsync(Client, _address, dataFlow);
            _deviceIds = devices.Select(d => d.Id).ToArray();

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
                var idx = Array.IndexOf(_deviceIds, currentId);
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