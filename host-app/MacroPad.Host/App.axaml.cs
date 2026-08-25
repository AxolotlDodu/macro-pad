using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace MacroPad.Host;

public partial class App : Application
{
    private CancellationTokenSource? _cts;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (Program.ShowConfig)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new ConfigWindow();

            base.OnFrameworkInitializationCompleted();
            return;
        }

        var trayIcons = TrayIcon.GetIcons(this);
        var trayIcon = trayIcons![0];
        trayIcon.Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://MacroPad.Host/Assets/tray.ico")));

        trayIcon.Clicked += (_, _) =>
        {
            _cts?.Cancel();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                lifetime.Shutdown();
        };

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => new PadService(_cts.Token).RunAsync(), _cts.Token);

        base.OnFrameworkInitializationCompleted();
    }
}