using System.Text.Json;
using HidSharp;

var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
var configJson = File.ReadAllText(configPath);
var config = JsonSerializer.Deserialize<Config>(configJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
             ?? new Config();

var sonarAddress = await SonarAddressResolver.GetSonarAddressAsync();
Console.WriteLine($"Adresse Sonar résolue : {sonarAddress ?? "AUCUNE"}");

var actions = new Dictionary<int, IAction>();
foreach (var (key, binding) in config.Bindings)
{
    if (binding.Target.Contains("{sonar}") && sonarAddress is not null)
        binding.Target = binding.Target.Replace("{sonar}", sonarAddress);

    var action = ActionFactory.Create(binding, sonarAddress);
    if (action is not null && int.TryParse(key, out var bit))
        actions[bit] = action;
}

Console.WriteLine($"{actions.Count} binding(s) chargé(s) :");
foreach (var (bit, _) in actions)
    Console.WriteLine($"  bit {bit} -> {config.Bindings.First(b => int.Parse(b.Key) == bit).Value.Type}");
Console.WriteLine();


const ushort VendorId = 0x2341;
const ushort ProductId = 0x8036;

while (true)
{
    var device = DeviceList.Local.GetHidDevices(vendorID: VendorId, productID: ProductId).FirstOrDefault();

    if (device is null)
    {
        Console.WriteLine("Pad non trouvé, nouvelle tentative dans 2s...");
        Thread.Sleep(2000);
        continue;
    }

    Console.WriteLine($"Pad connecté : {device.DevicePath}");

    try
    {
        ListenToDevice(device, actions);
    }
    catch (Exception ex) when (ex is IOException or TimeoutException or ObjectDisposedException)
    {
        Console.WriteLine("Pad déconnecté. En attente de reconnexion...");
    }
}

void ListenToDevice(HidDevice device, Dictionary<int, IAction> actions)
{
    using var stream = device.Open();
    stream.ReadTimeout = Timeout.Infinite;

    var buffer = new byte[device.GetMaxInputReportLength()];
    ushort lastMask = 0;

    while (true)
    {
        int count = stream.Read(buffer, 0, buffer.Length);
        if (count == 0) throw new IOException("Rapport vide, device probablement déconnecté");

        int offset = count > 2 ? 1 : 0;
        ushort mask = (ushort)(buffer[offset] | (buffer[offset + 1] << 8));

        Console.WriteLine($"Rapport reçu, mask={Convert.ToString(mask, 2).PadLeft(12, '0')}");

        if (mask != lastMask)
        {
            ushort changed = (ushort)(mask ^ lastMask);
            for (int bit = 0; bit < 12; bit++)
            {
                bool bitChanged = (changed & (1 << bit)) != 0;
                bool nowPressed = (mask & (1 << bit)) != 0;

                if (bitChanged && nowPressed && actions.TryGetValue(bit, out var action))
                    {
                        Console.WriteLine($"Bouton {bit} → exécution de l'action");
                        try
                        {
                            action.Execute();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erreur lors de l'exécution : {ex.Message}");
                        }
                    }
            }
            lastMask = mask;
        }
    }
}