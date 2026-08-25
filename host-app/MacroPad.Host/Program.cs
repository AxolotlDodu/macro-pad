using Avalonia;

namespace MacroPad.Host;

class Program
{
    public static bool ShowConfig;

    [STAThread]
    public static void Main(string[] args)
    {
        ShowConfig = args.Contains("--config");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}