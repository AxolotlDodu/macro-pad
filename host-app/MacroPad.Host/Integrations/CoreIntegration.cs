using MacroPad.Host;

namespace MacroPad.Host.Integrations;

public class CoreIntegration : IIntegration
{
    public string Key => "core";
    public bool IsAvailable => true;
    public Task InitializeAsync() => Task.CompletedTask;

    public IReadOnlyList<ActionTypeDescriptor> ButtonActionTypes { get; } = new List<ActionTypeDescriptor>
    {
        new() { Id = "launch", Label = "Lancer un programme", Target = TargetKind.FreeText },
        new() { Id = "url", Label = "Ouvrir une URL", Target = TargetKind.FreeText },
        new() { Id = "keystroke", Label = "Raccourci clavier", Target = TargetKind.FreeText },
        new() { Id = "http", Label = "Requête HTTP", Target = TargetKind.FreeText, SupportsHttpMethod = true },
        new() { Id = "switch-page", Label = "Changer de page", Target = TargetKind.PageCombo },
    };

    public IReadOnlyList<ActionTypeDescriptor> EncoderActionTypes { get; } = Array.Empty<ActionTypeDescriptor>();

    public IAction? CreateAction(string type, BindingConfig config, IntegrationContext context) => type switch
    {
        "launch" => new LaunchAction(config.Target),
        "url" => new UrlAction(config.Target),
        "keystroke" => new KeystrokeAction(config.Target),
        "http" => new HttpAction(config.Method, config.Target, config.Body),
        "switch-page" => context.PageSwitcher is not null ? new SwitchPageAction(context.PageSwitcher, config.Target) : null,
        _ => null
    };

    public IEncoderAction? CreateEncoderAction(string type, EncoderConfig config, IntegrationContext context) => null;
}