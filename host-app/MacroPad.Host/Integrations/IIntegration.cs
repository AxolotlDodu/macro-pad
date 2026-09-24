using MacroPad.Host;

namespace MacroPad.Host.Integrations;

public record IntegrationContext(IPageSwitcher? PageSwitcher, IPadNotifier? Notifier);

public interface IIntegration
{
    string Key { get; }
    bool IsAvailable { get; }
    Task InitializeAsync();

    IReadOnlyList<ActionTypeDescriptor> ButtonActionTypes { get; }
    IReadOnlyList<ActionTypeDescriptor> EncoderActionTypes { get; }

    IAction? CreateAction(string type, BindingConfig config, IntegrationContext context);
    IEncoderAction? CreateEncoderAction(string type, EncoderConfig config, IntegrationContext context);
}