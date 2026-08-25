using System.Text.Json;
using HidSharp;

namespace MacroPad.Host;

public class PadService
{
    private const ushort VendorId = 0x2341;
    private const ushort ProductId = 0x8036;
    private readonly CancellationToken _token;

    public PadService(CancellationToken token) => _token = token;

    public async Task RunAsync()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Config();

        var sonarAddress = await SonarAddressResolver.GetSonarAddressAsync();

        var actions = new Dictionary<int, IAction>();
        foreach (var (key, binding) in config.Bindings)
        {
            if (binding.Target.Contains("{sonar}") && sonarAddress is not null)
                binding.Target = binding.Target.Replace("{sonar}", sonarAddress);

            var action = ActionFactory.Create(binding, sonarAddress);
            if (action is not null && int.TryParse(key, out var bit))
                actions[bit] = action;
        }

        while (!_token.IsCancellationRequested)
        {
            var device = DeviceList.Local.GetHidDevices(vendorID: VendorId, productID: ProductId).FirstOrDefault();

            if (device is null)
            {
                await Task.Delay(2000, _token);
                continue;
            }

            try
            {
                await ListenAsync(device, actions);
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or ObjectDisposedException) { }
        }
    }

    private async Task ListenAsync(HidDevice device, Dictionary<int, IAction> actions)
    {
        using var stream = device.Open();
        stream.ReadTimeout = Timeout.Infinite;
        var buffer = new byte[device.GetMaxInputReportLength()];
        ushort lastMask = 0;

        while (!_token.IsCancellationRequested)
        {
            int count = stream.Read(buffer, 0, buffer.Length);
            if (count == 0) throw new IOException("Rapport vide");

            int offset = count > 2 ? 1 : 0;
            ushort mask = (ushort)(buffer[offset] | (buffer[offset + 1] << 8));

            if (mask != lastMask)
            {
                ushort changed = (ushort)(mask ^ lastMask);
                for (int bit = 0; bit < 12; bit++)
                {
                    if ((changed & (1 << bit)) != 0 && (mask & (1 << bit)) != 0
                        && actions.TryGetValue(bit, out var action))
                    {
                        try { action.Execute(); }
                        catch (Exception ex) { Console.WriteLine($"Erreur action bit {bit} : {ex.Message}"); }
                    }
                }
                lastMask = mask;
            }
        }
    }
}