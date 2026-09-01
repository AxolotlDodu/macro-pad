namespace MacroPad.Host;

public class DiscordDeafenToggleAction : IAction
{
    private readonly DiscordRpcClient _client;

    public DiscordDeafenToggleAction(DiscordRpcClient client) => _client = client;

    public void Execute() => _ = ToggleAsync();

    private async Task ToggleAsync()
    {
        if (!await _client.EnsureReadyAsync())
        {
            Console.WriteLine("[DiscordDeafen] Discord RPC indisponible.");
            return;
        }

        var current = await _client.GetVoiceSettingsAsync();
        if (current is null) return;
        var (_, deaf) = current.Value;

        bool newDeaf = !deaf;
        bool newMute = newDeaf; // deaf implique toujours mute

        var data = await _client.SetVoiceSettingsAsync(mute: newMute, deaf: newDeaf);
        if (data is null) return;

        Console.WriteLine($"[DiscordDeafen] mute={data.Value.GetProperty("mute").GetBoolean()} deaf={data.Value.GetProperty("deaf").GetBoolean()}");
    }
}