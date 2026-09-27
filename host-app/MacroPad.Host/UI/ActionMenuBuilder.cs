using MacroPad.Host.Integrations;

namespace MacroPad.Host;

/// <summary>Item du menu déroulant "niveau 1" (intégration) partagé par BindingEditWindow
/// et EncoderEditWindow : les actions core restent directement sélectionnables, les autres
/// intégrations (Sonar, Discord, Soundboard...) sont regroupées sous une seule entrée.</summary>
public class ActionMenuItem
{
    public required string Display { get; init; }
    public string? DirectTypeId { get; init; }
    public string? IntegrationKey { get; init; }
    public override string ToString() => Display;
}

public static class ActionMenuBuilder
{
    public static List<ActionMenuItem> BuildTopLevel(IReadOnlyList<ActionTypeDescriptor> descriptors)
    {
        var items = new List<ActionMenuItem> { new() { Display = "Aucune", DirectTypeId = "none" } };

        items.AddRange(descriptors
            .Where(d => d.IntegrationKey == "core")
            .Select(d => new ActionMenuItem { Display = d.Label, DirectTypeId = d.Id }));

        items.AddRange(descriptors
            .Where(d => d.IntegrationKey != "core")
            .Select(d => d.IntegrationKey)
            .Distinct()
            .Select(key => new ActionMenuItem { Display = DisplayNameFor(key), IntegrationKey = key }));

        return items;
    }

    public static List<ActionTypeDescriptor> ForGroup(IReadOnlyList<ActionTypeDescriptor> descriptors, string integrationKey) =>
        descriptors.Where(d => d.IntegrationKey == integrationKey).ToList();

    public static ActionTypeDescriptor? Find(IReadOnlyList<ActionTypeDescriptor> descriptors, string typeId) =>
        descriptors.FirstOrDefault(d => d.Id == typeId);

    /// <summary>Retrouve l'item de niveau 1 correspondant à un type déjà enregistré en config,
    /// pour resélectionner correctement à l'ouverture de la fenêtre d'édition.</summary>
    public static ActionMenuItem? ResolveTopLevel(List<ActionMenuItem> topLevel, IReadOnlyList<ActionTypeDescriptor> descriptors, string currentType)
    {
        if (currentType == "none") return topLevel.FirstOrDefault(i => i.DirectTypeId == "none");

        var descriptor = Find(descriptors, currentType);
        if (descriptor is null) return topLevel.FirstOrDefault(i => i.DirectTypeId == "none");

        return descriptor.IntegrationKey == "core"
            ? topLevel.FirstOrDefault(i => i.DirectTypeId == currentType)
            : topLevel.FirstOrDefault(i => i.IntegrationKey == descriptor.IntegrationKey);
    }

    private static string DisplayNameFor(string integrationKey) => integrationKey switch
    {
        "sonar" => "Sonar",
        "discord" => "Discord",
        "soundboard" => "Soundboard",
        _ => integrationKey
    };
}