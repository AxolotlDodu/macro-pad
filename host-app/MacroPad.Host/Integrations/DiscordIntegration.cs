using MacroPad.Host;

namespace MacroPad.Host.Integrations;

public class DiscordIntegration : IIntegration
{
    public string Key => "discord";
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private DiscordRpcClient? _client;

    public DiscordIntegration(string? clientId, string? clientSecret)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    public bool IsAvailable => _client is not null;

    public Task InitializeAsync()
    {
        if (_clientId is not null && _clientSecret is not null)
            _client = new DiscordRpcClient(_clientId, _clientSecret);
        else
            Console.WriteLine("[Integration:discord] Pas de clientId/secret configuré, intégration désactivée.");
        return Task.CompletedTask;
    }

    public IReadOnlyList<ActionTypeDescriptor> ButtonActionTypes { get; } = new List<ActionTypeDescriptor>
    {
        new() { Id = "discord-toggle-mute", Label = "Mute/Unmute", Target = TargetKind.None, IntegrationKey = "discord" },
        new() { Id = "discord-toggle-deafen", Label = "Deafen", Target = TargetKind.None, IntegrationKey = "discord" },
    };

    public IReadOnlyList<ActionTypeDescriptor> EncoderActionTypes { get; } = Array.Empty<ActionTypeDescriptor>();

    public IAction? CreateAction(string type, BindingConfig config, IntegrationContext context)
    {
        if (_client is null) return null;
        return type switch
        {
            "discord-toggle-mute" => new DiscordMuteToggleAction(_client, context.Notifier),
            "discord-toggle-deafen" => new DiscordDeafenToggleAction(_client, context.Notifier),
            _ => null
        };
    }

    public IEncoderAction? CreateEncoderAction(string type, EncoderConfig config, IntegrationContext context) => null;
}