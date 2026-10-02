namespace MacroPad.Host;

public class DiscordDeafenToggleAction : IAction
{
    private readonly DiscordRpcClient _client;

    private readonly IPadNotifier? _notifier;

    public DiscordDeafenToggleAction(DiscordRpcClient client, IPadNotifier? notifier = null)
    {
        _client = client;
        _notifier = notifier;
    }

    public void Execute() => _ = ToggleAsync();

    private async Task ToggleAsync()
    {
        if (!await _client.EnsureReadyAsync())
        {
            Console.WriteLine("[DiscordDeafen] Discord RPC indisponible.");
            _notifier?.ShowNotification("Discord : indisponible");
            return;
        }

        var current = await _client.GetVoiceSettingsAsync();
        if (current is null) return;
        var (_, deaf) = current.Value;

        bool newDeaf = !deaf;
        bool newMute = newDeaf; // deaf implique toujours mute

        var data = await _client.SetVoiceSettingsAsync(mute: newMute, deaf: newDeaf);
        if (data is null) return;

        var isDeaf = data.Value.GetProperty("deaf").GetBoolean();
        Console.WriteLine($"[DiscordDeafen] mute={data.Value.GetProperty("mute").GetBoolean()} deaf={isDeaf}");
        _notifier?.ShowNotification($"Discord : {(isDeaf ? "Deafen" : "Audio actif")}");
    }
}