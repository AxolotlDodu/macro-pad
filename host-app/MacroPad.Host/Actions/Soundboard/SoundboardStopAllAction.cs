using MacroPad.Soundboard;

namespace MacroPad.Host.Integrations;

public class SoundboardStopAllAction : IAction
{
    private readonly SoundboardClient _client;

    private readonly IPadNotifier? _notifier;

    public SoundboardStopAllAction(SoundboardClient client, IPadNotifier? notifier = null)
    {
        _client = client;
        _notifier = notifier;
    }

    public void Execute()
    {
        _ = _client.StopAllAsync();
        _notifier?.ShowNotification("Soundboard : Stop");
    }
}