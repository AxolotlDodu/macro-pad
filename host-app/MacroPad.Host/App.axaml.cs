using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

public partial class App : Application
{
    private CancellationTokenSource? _cts;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Fenêtre invisible requise sous Windows pour que le menu du tray icon
            // ait un handle valide auquel s'accrocher.
            desktop.MainWindow = new Window
            {
                Width = 0,
                Height = 0,
                ShowInTaskbar = false,
                WindowDecorations = Avalonia.Controls.WindowDecorations.None,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Position = new PixelPoint(-10000, -10000)
            };
        }

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => new PadService(_cts.Token).RunAsync(), _cts.Token);

        var quitItem = new NativeMenuItem("Quitter");
        quitItem.Click += (_, _) =>
        {
            _cts?.Cancel();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                lifetime.Shutdown();
        };

        var trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://MacroPad.Host/Assets/tray.ico"))),
            ToolTipText = "MacroPad",
            Menu = new NativeMenu { quitItem }
        };

        var trayIcons = new TrayIcons { trayIcon };
        TrayIcon.SetIcons(this, trayIcons);

        base.OnFrameworkInitializationCompleted();
    }
}