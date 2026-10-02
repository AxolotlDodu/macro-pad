using System.Diagnostics;

namespace MacroPad.Host;
public class LaunchAction : IAction
{
    private readonly string _target;
    private readonly IPadNotifier? _notifier;

    public LaunchAction(string target, IPadNotifier? notifier = null)
    {
        _target = target;
        _notifier = notifier;
    }

    public void Execute()
    {
        Process.Start(new ProcessStartInfo(_target) { UseShellExecute = true });

        var name = Path.GetFileNameWithoutExtension(_target);
        _notifier?.ShowNotification($"Lancement : {(string.IsNullOrEmpty(name) ? _target : name)}");
    }
}