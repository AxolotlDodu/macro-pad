using MacroPad.Soundboard;

namespace MacroPad.Host.Integrations;

public class SoundboardPlayAction : IAction
{
    private readonly SoundboardClient _client;
    private readonly string _soundId;

    private readonly string _displayName;
    private readonly IPadNotifier? _notifier;

    public SoundboardPlayAction(SoundboardClient client, string soundId, string displayName, IPadNotifier? notifier = null)
    {
        _client = client;
        _soundId = soundId;
        _displayName = displayName;
        _notifier = notifier;
    }

    public void Execute()
    {
        _ = _client.PlaySoundAsync(_soundId);
        _notifier?.ShowNotification($"Son : {_displayName}");
    }
}