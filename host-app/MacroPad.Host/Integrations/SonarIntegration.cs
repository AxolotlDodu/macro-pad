using MacroPad.Host;

namespace MacroPad.Host.Integrations;

public class SonarIntegration : IIntegration
{
    public string Key => "sonar";
    public string? Address { get; private set; }
    public bool IsAvailable => Address is not null;

    public async Task InitializeAsync()
    {
        Address = await SonarAddressResolver.GetSonarAddressAsync();
        if (Address is null)
            Console.WriteLine("[Integration:sonar] Adresse Sonar introuvable, intégration désactivée.");
    }

    public IReadOnlyList<ActionTypeDescriptor> ButtonActionTypes { get; } = new List<ActionTypeDescriptor>
    {
        new() { Id = "sonar-toggle-mute", Label = "Sonar : Mute/Unmute channel", Target = TargetKind.ChannelCombo,
                ComboOptions = SonarChannels.All, ComboDisplayName = SonarChannels.GetDisplayName },
        new() { Id = "sonar-set-output", Label = "Sonar : Définir la sortie", Target = TargetKind.FreeText },
        new() { Id = "sonar-set-mic", Label = "Sonar : Définir le micro", Target = TargetKind.FreeText },
        new() { Id = "sonar-cycle-output", Label = "Sonar : Cycler la sortie", Target = TargetKind.None },
        new() { Id = "sonar-cycle-mic", Label = "Sonar : Cycler le micro", Target = TargetKind.None },
    };

    public IReadOnlyList<ActionTypeDescriptor> EncoderActionTypes { get; } = new List<ActionTypeDescriptor>
    {
        new() { Id = "sonar-volume", Label = "Sonar : Volume channel", Target = TargetKind.ChannelCombo,
                ComboOptions = SonarChannels.All, ComboDisplayName = SonarChannels.GetDisplayName },
    };

    public IAction? CreateAction(string type, BindingConfig config, IntegrationContext context)
    {
        if (Address is null) return null;
        return type switch
        {
            "sonar-toggle-mute" => new SonarToggleMuteAction(Address, config.Target, context.Notifier),
            "sonar-set-output" => new SonarSetDeviceAction(Address, "render", config.Target, context.Notifier),
            "sonar-set-mic" => new SonarSetDeviceAction(Address, "mic", config.Target, context.Notifier),
            "sonar-cycle-output" => new SonarCycleDeviceAction(Address, "render", config.Devices, context.Notifier),
            "sonar-cycle-mic" => new SonarCycleDeviceAction(Address, "mic", config.Devices, context.Notifier),
            _ => null
        };
    }

    public IEncoderAction? CreateEncoderAction(string type, EncoderConfig config, IntegrationContext context)
    {
        if (Address is null) return null;
        return type switch
        {
            "sonar-volume" => new SonarVolumeEncoderAction(Address, config.Target, config.Step > 0 ? config.Step : 0.05, context.Notifier),
            _ => null
        };
    }
}