using MacroPad.Soundboard;

namespace MacroPad.Host.Integrations;

public class SoundboardStopAllAction : IAction
{
    private readonly SoundboardClient _client;

    public SoundboardStopAllAction(SoundboardClient client) => _client = client;

    public void Execute() => _ = _client.StopAllAsync();
}