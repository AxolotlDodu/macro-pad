using MacroPad.Soundboard;

namespace MacroPad.Host.Integrations;

public class SoundboardPlayAction : IAction
{
    private readonly SoundboardClient _client;
    private readonly string _soundId;

    public SoundboardPlayAction(SoundboardClient client, string soundId)
    {
        _client = client;
        _soundId = soundId;
    }

    public void Execute() => _ = _client.PlaySoundAsync(_soundId);
}