using System.Diagnostics;

public class LaunchAction : IAction
{
    private readonly string _target;
    public LaunchAction(string target) => _target = target;

    public void Execute()
    {
        Process.Start(new ProcessStartInfo(_target) { UseShellExecute = true });
    }
}