using System.Diagnostics;

public class UrlAction : IAction
{
    private readonly string _target;
    public UrlAction(string target) => _target = target;

    public void Execute()
    {
        Process.Start(new ProcessStartInfo(_target) { UseShellExecute = true });
    }
}