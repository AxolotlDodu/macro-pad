using HidSharp;

var candidates = DeviceList.Local.GetHidDevices(vendorID: 0x2341, productID: 0x8036).ToList();

Console.WriteLine($"{candidates.Count} interface(s) trouvée(s) pour ce VID/PID :\n");
foreach (var d in candidates)
{
    Console.WriteLine($"  {d.DevicePath}");
}
Console.WriteLine();

var device = candidates.FirstOrDefault(d => !d.DevicePath.Contains("kbd", StringComparison.OrdinalIgnoreCase));

if (device is null)
{
    Console.WriteLine("Interface Raw HID non trouvée (seule l'interface clavier standard a été détectée).");
    return;
}

Console.WriteLine($"Ouverture de : {device.DevicePath}\n");

using var stream = device.Open();
stream.ReadTimeout = Timeout.Infinite;

Console.WriteLine($"MaxInputReportLength: {device.GetMaxInputReportLength()}");
Console.WriteLine($"MaxOutputReportLength: {device.GetMaxOutputReportLength()}");

var buffer = new byte[device.GetMaxInputReportLength()];
ushort lastMask = 0;

Console.WriteLine("En écoute... appuie sur chaque bouton un par un (Ctrl+C pour quitter)\n");

while (true)
{
    int count;
    try
    {
        count = stream.Read(buffer, 0, buffer.Length);
    }
    catch (TimeoutException)
    {
        continue;
    }

    int offset = count > 2 ? 1 : 0;
    ushort mask = (ushort)(buffer[offset] | (buffer[offset + 1] << 8));

    if (mask != lastMask)
    {
        ushort changed = (ushort)(mask ^ lastMask);
        for (int bit = 0; bit < 12; bit++)
        {
            if ((changed & (1 << bit)) != 0)
            {
                bool pressed = (mask & (1 << bit)) != 0;
                Console.WriteLine($"Bit {bit}: {(pressed ? "PRESSÉ" : "relâché")}");
            }
        }
        lastMask = mask;
    }
}