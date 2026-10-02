namespace MacroPad.Host;

public class DiscordMuteToggleAction : IAction
{
    private readonly DiscordRpcClient _client;

    private readonly IPadNotifier? _notifier;

    public DiscordMuteToggleAction(DiscordRpcClient client, IPadNotifier? notifier = null)
    {
        _client = client;
        _notifier = notifier;
    }

    public void Execute() => _ = ToggleAsync();

    private async Task ToggleAsync()
    {
        if (!await _client.EnsureReadyAsync())
        {
            Console.WriteLine("[DiscordMute] Discord RPC indisponible.");
            _notifier?.ShowNotification("Discord : indisponible");
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

        var isMuted = data.Value.GetProperty("mute").GetBoolean();
        Console.WriteLine($"[DiscordMute] mute={isMuted} deaf={data.Value.GetProperty("deaf").GetBoolean()}");
        _notifier?.ShowNotification($"Discord : {(isMuted ? "Mute" : "Micro actif")}");
    }
}