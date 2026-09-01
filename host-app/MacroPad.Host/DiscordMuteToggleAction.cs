namespace MacroPad.Host;

public class DiscordMuteToggleAction : IAction
{
    private readonly DiscordRpcClient _client;
    private bool? _lastKnownMuted;

    public DiscordMuteToggleAction(DiscordRpcClient client) => _client = client;

    public void Execute() => _ = ToggleAsync();

    private async Task ToggleAsync()
    {
        if (!await _client.EnsureReadyAsync())
        {
            Console.WriteLine("[DiscordMute] Discord RPC indisponible.");
            return;
        }

        var desiredMuted = !(_lastKnownMuted ?? false);
        var data = await _client.SetVoiceSettingsAsync(mute: desiredMuted, deaf: false);
        if (data is null) return;

        var actualMuted = data.Value.GetProperty("mute").GetBoolean();
        _lastKnownMuted = actualMuted;
        Console.WriteLine($"[DiscordMute] muted={actualMuted}");
    }
}