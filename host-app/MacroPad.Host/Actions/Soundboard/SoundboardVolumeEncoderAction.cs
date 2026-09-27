using MacroPad.Soundboard;

namespace MacroPad.Host.Integrations;

public class SoundboardVolumeEncoderAction : IEncoderAction
{
    private readonly SoundboardClient _client;
    private readonly int _stepPercent;
    private readonly IPadNotifier? _notifier;

    public SoundboardVolumeEncoderAction(SoundboardClient client, int stepPercent = 5, IPadNotifier? notifier = null)
    {
        _client = client;
        _stepPercent = stepPercent;
        _notifier = notifier;
    }

    public void Execute(int ticks) => _ = ExecuteAsync(ticks);

    private async Task ExecuteAsync(int ticks)
    {
        int delta = ticks * _stepPercent;
        await _client.AdjustVolumeAsync(delta);

        if (_notifier is null) return;
        var volume = await _client.GetVolumeAsync();
        if (volume is not null)
            _notifier.ShowNotification($"Soundboard: {(int)Math.Round(volume.Value * 100)}%");
    }
}