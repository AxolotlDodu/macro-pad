using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MacroPad.Host;

/// <summary>
/// Sonde périodiquement la fenêtre au premier plan et bascule vers la première page dont
/// un AppMatchers correspond au nom du processus. Repose sur des API Win32 (P/Invoke) :
/// sur Linux, RunAsync se termine immédiatement sans rien faire (extension future possible
/// via un mécanisme équivalent type wmctrl/X11).
/// </summary>
public class AppFocusWatcher
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private readonly List<PageConfig> _pages;
    private readonly IPageSwitcher _pageSwitcher;
    private readonly CancellationToken _token;
    private string? _lastProcess;

    public AppFocusWatcher(List<PageConfig> pages, IPageSwitcher pageSwitcher, CancellationToken token)
    {
        _pages = pages;
        _pageSwitcher = pageSwitcher;
        _token = token;
    }

    public async Task RunAsync()
    {
        if (!OperatingSystem.IsWindows()) return;
        if (_pages.All(p => p.AppMatchers.Count == 0)) return; // rien à surveiller

        while (!_token.IsCancellationRequested)
        {
            try
            {
                var processName = GetForegroundProcessName();
                if (processName is not null && processName != _lastProcess)
                {
                    _lastProcess = processName;

                    var match = _pages.FirstOrDefault(p => p.AppMatchers.Any(m =>
                        processName.Contains(m, StringComparison.OrdinalIgnoreCase)));

                    if (match is not null)
                        _pageSwitcher.SwitchToPage(match.Name);
                }
            }
            catch
            {
                // Fenêtre transitoire (ex: process qui vient de se fermer) — on ignore et on réessaie.
            }

            await Task.Delay(750, _token);
        }
    }

    private static string? GetForegroundProcessName()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero) return null;

        GetWindowThreadProcessId(hWnd, out var pid);
        if (pid == 0) return null;

        using var proc = Process.GetProcessById((int)pid);
        return proc.ProcessName;
    }
}
