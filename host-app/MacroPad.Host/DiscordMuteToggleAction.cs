namespace MacroPad.Host;

public class DiscordMuteToggleAction : IAction
{
    private readonly DiscordRpcClient _client;

    public DiscordMuteToggleAction(DiscordRpcClient client) => _client = client;

    public void Execute() => _ = ToggleAsync();

    private async Task ToggleAsync()
    {
        if (!await _client.EnsureReadyAsync())
        {
            Console.WriteLine("[DiscordMute] Discord RPC indisponible.");
            return;
        }

        var current = await _client.GetVoiceSettingsAsync();
        if (current is null) return;
        var (mute, deaf) = current.Value;

        // Si deafen actif, le bouton mute retire tout (comme le client natif).
        bool newMute = deaf ? false : !mute;
        bool newDeaf = false;

        var data = await _client.SetVoiceSettingsAsync(mute: newMute, deaf: newDeaf);
        if (data is null) return;

        Console.WriteLine($"[DiscordMute] mute={data.Value.GetProperty("mute").GetBoolean()} deaf={data.Value.GetProperty("deaf").GetBoolean()}");
    }
}