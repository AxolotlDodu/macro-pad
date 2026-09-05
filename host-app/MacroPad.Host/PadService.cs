using HidSharp;

namespace MacroPad.Host;

public class PadService : IPadNotifier
{
    private const ushort VendorId = 0x2341;
    private const ushort ProductId = 0x8036;

    /// <summary>Bit du bouton pin 14 (10e switch). Verrouillé sur le changement de page,
    /// quelle que soit la config chargée — voir RunAsync.</summary>
    private const int ReservedPageSwitchBit = 9;

    private const byte CmdSetNotification = 0x03;

    private readonly CancellationToken _token;
    private IPageSwitcher _pageSwitcher = null!;
    private string _configPath = "";
    private string? _sonarAddress;
    private DiscordRpcClient? _discordClient;

    private readonly Dictionary<int, DateTime> _lastBitTrigger = new();
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(150);

    private List<PageConfig> _pages = new();
    private Dictionary<string, Dictionary<int, IAction>> _bitActionsByPage = new();
    private Dictionary<string, Dictionary<int, IEncoderAction>> _encoderActionsByPage = new();

    private readonly object _activeLock = new();
    private Dictionary<int, IAction> _activeBitActions = new();
    private Dictionary<int, IEncoderAction> _activeEncoderActions = new();

    private readonly object _reloadLock = new();

    // Stream d'écriture vers le MacroPad (écran)
    private HidStream? _outputStream;
    private readonly object _streamLock = new();

    public PadService(CancellationToken token) => _token = token;

    public async Task RunAsync()
    {
        _configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        var config = Config.Load(_configPath);
        _sonarAddress = await SonarAddressResolver.GetSonarAddressAsync();
        _discordClient = config.DiscordClientId is not null
            ? new DiscordRpcClient(config.DiscordClientId, config.DiscordClientSecret!)
            : null;

        _pageSwitcher = new PageSwitcher(config.Pages);
        ApplyConfig(config);

        _pageSwitcher.PageChanged += SetActivePage;

        _ = Task.Run(() => new AppFocusWatcher(() => _pages, _pageSwitcher, _token).RunAsync(), _token);
        _ = Task.Run(() => TimeBroadcastLoopAsync(), _token);

        StartConfigWatcher();

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
                await ListenAsync(device);
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or ObjectDisposedException) { }
        }
    }

    /// <summary>Reconstruit les dictionnaires d'actions à partir d'une config donnée.
    /// Appelé au démarrage et à chaque rechargement à chaud.</summary>
    private void ApplyConfig(Config config)
    {
        var bitActionsByPage = new Dictionary<string, Dictionary<int, IAction>>();
        var encoderActionsByPage = new Dictionary<string, Dictionary<int, IEncoderAction>>();
        var cycleAction = new SwitchPageAction(_pageSwitcher, null);

        foreach (var page in config.Pages)
        {
            var bitActions = new Dictionary<int, IAction>();
            foreach (var (key, binding) in page.Bindings)
            {
                if (!int.TryParse(key, out var bit)) continue;

                if (bit == ReservedPageSwitchBit)
                {
                    Console.WriteLine($"[PadService] Page \"{page.Name}\" : binding sur le bit {ReservedPageSwitchBit} ignoré (réservé au changement de page).");
                    continue;
                }

                if (binding.Target.Contains("{sonar}") && _sonarAddress is not null)
                    binding.Target = binding.Target.Replace("{sonar}", _sonarAddress);

                var action = ActionFactory.Create(binding, _sonarAddress, _pageSwitcher, _discordClient, this);
                if (action is not null) bitActions[bit] = action;
            }

            bitActions[ReservedPageSwitchBit] = cycleAction;

            var encoderActions = new Dictionary<int, IEncoderAction>();
            foreach (var (key, enc) in page.Encoders)
            {
                if (!int.TryParse(key, out var idx)) continue;
                var action = ActionFactory.CreateEncoder(enc, _sonarAddress, this);
                if (action is not null) encoderActions[idx] = action;
            }

            bitActionsByPage[page.Name] = bitActions;
            encoderActionsByPage[page.Name] = encoderActions;
        }

        _pages = config.Pages;
        _bitActionsByPage = bitActionsByPage;
        _encoderActionsByPage = encoderActionsByPage;

        _pageSwitcher.UpdatePages(config.Pages);
        SetActivePage(_pageSwitcher.CurrentPageName);
    }

    private void StartConfigWatcher()
    {
        var dir = Path.GetDirectoryName(_configPath)!;
        var watcher = new FileSystemWatcher(dir, Path.GetFileName(_configPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        watcher.Changed += async (_, _) => await ReloadConfigAsync();
        _token.Register(() => watcher.Dispose());
    }

    private async Task ReloadConfigAsync()
    {
        try { await Task.Delay(300, _token); } catch (OperationCanceledException) { return; }

        lock (_reloadLock)
        {
            try
            {
                var config = Config.Load(_configPath);
                if (config.Pages.Count == 0) return;
                ApplyConfig(config);
                Console.WriteLine("[PadService] Configuration rechargée à chaud.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PadService] Échec du rechargement à chaud : {ex.Message}");
            }
        }
    }

    private void SetActivePage(string pageName)
    {
        lock (_activeLock)
        {
            _activeBitActions = _bitActionsByPage.GetValueOrDefault(pageName, new Dictionary<int, IAction>());
            _activeEncoderActions = _encoderActionsByPage.GetValueOrDefault(pageName, new Dictionary<int, IEncoderAction>());
        }
        Console.WriteLine($"[PadService] Page active : {pageName}");

        var index = _pages.FindIndex(p => p.Name == pageName);
        if (index >= 0) SendPageInfo(pageName, index + 1, _pages.Count);
    }

    private void SendPageInfo(string name, int index, int total)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(name);
        var len = Math.Min(bytes.Length, 20);

        var report = new byte[64];
        report[0] = 0x01; // CMD_SET_PAGE
        report[1] = (byte)index;
        report[2] = (byte)total;
        report[3] = (byte)len;
        Array.Copy(bytes, 0, report, 4, len);

        WriteOutputReport(report);
    }

    public void ShowNotification(string text)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(text);
        var len = Math.Min(bytes.Length, 20);

        var report = new byte[64];
        report[0] = CmdSetNotification;
        report[1] = (byte)len;
        Array.Copy(bytes, 0, report, 2, len);

        WriteOutputReport(report);
    }

    private async Task TimeBroadcastLoopAsync()
    {
        while (!_token.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var report = new byte[64];
            report[0] = 0x02; // CMD_SET_TIME
            report[1] = (byte)now.Hour;
            report[2] = (byte)now.Minute;
            WriteOutputReport(report);

            var delay = 60 - now.Second;
            await Task.Delay(TimeSpan.FromSeconds(delay), _token);
        }
    }

    private void WriteOutputReport(byte[] report)
    {
        HidStream? s;
        lock (_streamLock) { s = _outputStream; }
        if (s is null) return;

        var framed = new byte[report.Length + 1];
        framed[0] = 0x00; // Report ID
        Array.Copy(report, 0, framed, 1, report.Length);

        try { s.Write(framed); }
        catch (Exception ex) { Console.WriteLine($"[PadService] Échec envoi rapport affichage : {ex.Message}"); }
    }

    private async Task ListenAsync(HidDevice device)
    {
        using var stream = device.Open();
        lock (_streamLock) { _outputStream = stream; }
        
        SendPageInfo(_pageSwitcher.CurrentPageName, _pages.FindIndex(p => p.Name == _pageSwitcher.CurrentPageName) + 1, _pages.Count);

        try
        {
            stream.ReadTimeout = Timeout.Infinite;
            var buffer = new byte[device.GetMaxInputReportLength()];
            ushort lastMask = 0;

            while (!_token.IsCancellationRequested)
            {
                int count = stream.Read(buffer, 0, buffer.Length);
                if (count == 0) throw new IOException("Rapport vide");

                int offset = count > 2 ? 1 : 0;
                ushort mask = (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
                sbyte enc1Delta = unchecked((sbyte)buffer[offset + 2]);
                sbyte enc2Delta = unchecked((sbyte)buffer[offset + 3]);

                Dictionary<int, IAction> bitActions;
                Dictionary<int, IEncoderAction> encoderActions;
                lock (_activeLock)
                {
                    bitActions = _activeBitActions;
                    encoderActions = _activeEncoderActions;
                }

                if (mask != lastMask)
                {
                    ushort changed = (ushort)(mask ^ lastMask);
                    for (int bit = 0; bit < 12; bit++)
                    {
                        if ((changed & (1 << bit)) != 0 && (mask & (1 << bit)) != 0
                            && bitActions.TryGetValue(bit, out var action))
                        {
                            var now = DateTime.UtcNow;
                            if (_lastBitTrigger.TryGetValue(bit, out var last) && now - last < DebounceWindow)
                                continue; // rebond ignoré

                            _lastBitTrigger[bit] = now;

                            try { action.Execute(); }
                            catch (Exception ex) { Console.WriteLine($"Erreur action bit {bit} : {ex.Message}"); }
                        }
                    }
                    lastMask = mask;
                }

                if (enc1Delta != 0 && encoderActions.TryGetValue(1, out var e1))
                    SafeExecute(() => e1.Execute(enc1Delta));

                if (enc2Delta != 0 && encoderActions.TryGetValue(2, out var e2))
                    SafeExecute(() => e2.Execute(enc2Delta));
            }
        }
        finally
        {
            lock (_streamLock) { if (_outputStream == stream) _outputStream = null; }
        }
    }

    private static void SafeExecute(Action action)
    {
        try { action(); }
        catch (Exception ex) { Console.WriteLine($"Erreur action encodeur : {ex.Message}"); }
    }
}