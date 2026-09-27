using MacroPad.Soundboard;

namespace MacroPad.Host.Integrations;

/// <summary>
/// Pas d'endpoint mute dédié côté Soundboard : on mute/démute comme le volume général,
/// en mémorisant le volume courant avant de le mettre à 0, puis en le restaurant.
/// </summary>
public class SoundboardMuteToggleAction : IAction
{
    private readonly SoundboardClient _client;
    private readonly IPadNotifier? _notifier;
    private double? _volumeBeforeMute;

    public SoundboardMuteToggleAction(SoundboardClient client, IPadNotifier? notifier)
    {
        _client = client;
        _notifier = notifier;
    }

    public void Execute() => _ = ToggleAsync();

    private async Task ToggleAsync()
    {
        var current = await _client.GetVolumeAsync() ?? 0;

        if (current > 0)
        {
            _volumeBeforeMute = current;
            await _client.SetVolumeAsync(0);
            _notifier?.ShowNotification("Soundboard: Mute");
        }
        else
        {
            var restore = _volumeBeforeMute ?? 1.0;
            await _client.SetVolumeAsync((int)Math.Round(restore * 100));
            _notifier?.ShowNotification($"Soundboard: {(int)Math.Round(restore * 100)}%");
        }
    }
}