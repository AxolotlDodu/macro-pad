using HidSharp;

var devices = DeviceList.Local.GetHidDevices();

Console.WriteLine("Périphériques HID détectés :\n");

foreach (var device in devices)
{
    Console.WriteLine($"{device.GetFriendlyName()}");
    Console.WriteLine($"  VID: 0x{device.VendorID:X4}  PID: 0x{device.ProductID:X4}");
    Console.WriteLine($"  Path: {device.DevicePath}");
    Console.WriteLine();
}

Console.WriteLine("Appuie sur une touche pour quitter...");
Console.ReadKey();