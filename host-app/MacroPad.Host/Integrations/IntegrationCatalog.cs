using MacroPad.Host;

namespace MacroPad.Host.Integrations;

/// <summary>
/// Point d'enregistrement unique. Pour ajouter une intégration : implémenter IIntegration
/// puis l'ajouter à la liste ci-dessous. L'UI (combobox) et PadService (exécution) en découlent
/// automatiquement, sans autre modification.
/// </summary>
public static class IntegrationCatalog
{
    public static IReadOnlyList<IIntegration> CreateAll(Config config) => new IIntegration[]
    {
        new CoreIntegration(),
        new SonarIntegration(),
        new DiscordIntegration(config.DiscordClientId, config.DiscordClientSecret),
    };
}