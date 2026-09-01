namespace MacroPad.Host;

public class DiscordDeafenToggleAction : IAction
{
    private readonly DiscordRpcClient _client;
    private bool? _lastKnownDeaf;

    public DiscordDeafenToggleAction(DiscordRpcClient client) => _client = client;

    public void Execute() => _ = ToggleAsync();

    private async Task ToggleAsync()
    {
        if (!await _client.EnsureReadyAsync())
        {
            Console.WriteLine("[DiscordDeafen] Discord RPC indisponible.");
            return;
        }

        var desiredDeaf = !(_lastKnownDeaf ?? false);

        // Discord refuse l'état deaf=true + mute=false (incohérent côté client natif).
        // Se mettre en sourdine coupe aussi le micro ; en sortir réactive les deux.
        var data = await _client.SetVoiceSettingsAsync(mute: desiredDeaf, deaf: desiredDeaf);
        if (data is null) return;

        var actualDeaf = data.Value.GetProperty("deaf").GetBoolean();
        _lastKnownDeaf = actualDeaf;
        Console.WriteLine($"[DiscordDeafen] deaf={actualDeaf}");
    }
}