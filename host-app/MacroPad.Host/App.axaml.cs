using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace MacroPad.Host;

public partial class App : Application
{
    private CancellationTokenSource? _cts;
    private ConfigWindow? _configWindow;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            desktopLifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        var config = Config.Load(configPath);

        if (!config.ProfileWasChosen)
        {
            var dlg = new ProfileSelectWindow(config.Profile);
            dlg.Closed += (_, _) =>
            {
                config.Profile = dlg.Result;
                config.PageSwitchBit = config.Profile == PadProfile.TenKeyScreen ? 9 : 11;
                config.Save(configPath);
                ContinueStartup(config);
            };

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop0)
                desktop0.MainWindow = dlg;

            dlg.Show();
        }
        else
        {
            ContinueStartup(config);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ContinueStartup(Config config)
    {
        if (Program.ShowConfig)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new ConfigWindow();
            return;
        }

        var trayIcons = TrayIcon.GetIcons(this);
        var trayIcon = trayIcons![0];
        trayIcon.Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://MacroPad.Host/Assets/tray.ico")));

        trayIcon.Clicked += (_, _) => OpenConfigWindow();

        var menu = new NativeMenu();

        var configItem = new NativeMenuItem("Configuration...");
        configItem.Click += (_, _) => OpenConfigWindow();
        menu.Add(configItem);

        menu.Add(new NativeMenuItemSeparator());

        var quitItem = new NativeMenuItem("Quitter");
        quitItem.Click += (_, _) =>
        {
            _cts?.Cancel();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                lifetime.Shutdown();
        };
        menu.Add(quitItem);

        trayIcon.Menu = menu;

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => new PadService(_cts.Token).RunAsync(), _cts.Token);
    }

    private void OpenConfigWindow()
    {
        if (_configWindow is not null)
        {
            _configWindow.Activate();
            return;
        }

        _configWindow = new ConfigWindow();
        _configWindow.Closed += (_, _) => _configWindow = null;
        _configWindow.Show();
    }
}