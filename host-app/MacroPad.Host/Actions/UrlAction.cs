using System.Diagnostics;

namespace MacroPad.Host;

public class UrlAction : IAction
{
    private readonly string _target;
    private readonly IPadNotifier? _notifier;

    public UrlAction(string target, IPadNotifier? notifier = null)
    {
        _target = target;
        _notifier = notifier;
    }

    public void Execute()
    {
        Process.Start(new ProcessStartInfo(_target) { UseShellExecute = true });

        var label = Uri.TryCreate(_target, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host) ? uri.Host : _target;
        _notifier?.ShowNotification($"Ouverture : {label}");
    }
}