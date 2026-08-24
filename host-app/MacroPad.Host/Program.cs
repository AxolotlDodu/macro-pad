using System.Text.Json;
using HidSharp;

var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
var configJson = File.ReadAllText(configPath);
var config = JsonSerializer.Deserialize<Config>(configJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
             ?? new Config();

var actions = new Dictionary<int, IAction>();
foreach (var (key, binding) in config.Bindings)
{
    var action = ActionFactory.Create(binding);
    if (action is not null && int.TryParse(key, out var bit))
        actions[bit] = action;
}

var device = DeviceList.Local.GetHidDevices(vendorID: 0x2341, productID: 0x8036).FirstOrDefault();
if (device is null)
{
    Console.WriteLine("Pad non trouvé. Vérifie qu'il est branché.");
    return;
}

using var stream = device.Open();
stream.ReadTimeout = Timeout.Infinite;

var buffer = new byte[device.GetMaxInputReportLength()];
ushort lastMask = 0;

Console.WriteLine("En écoute...\n");

while (true)
{
    int count;
    try { count = stream.Read(buffer, 0, buffer.Length); }
    catch (TimeoutException) { continue; }

    int offset = count > 2 ? 1 : 0;
    ushort mask = (ushort)(buffer[offset] | (buffer[offset + 1] << 8));

    if (mask != lastMask)
    {
        ushort changed = (ushort)(mask ^ lastMask);
        for (int bit = 0; bit < 12; bit++)
        {
            bool bitChanged = (changed & (1 << bit)) != 0;
            bool nowPressed = (mask & (1 << bit)) != 0;

            // On déclenche seulement au moment où le bouton est pressé, pas relâché
            if (bitChanged && nowPressed && actions.TryGetValue(bit, out var action))
            {
                Console.WriteLine($"Bouton {bit} → exécution de l'action");
                action.Execute();
            }
        }
        lastMask = mask;
    }
}