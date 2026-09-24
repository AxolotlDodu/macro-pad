namespace MacroPad.Host.Integrations;

public enum TargetKind
{
    None,
    FreeText,
    ChannelCombo,
    PageCombo
}

public class ActionTypeDescriptor
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public TargetKind Target { get; init; } = TargetKind.None;
    public bool SupportsHttpMethod { get; init; }
    public IReadOnlyList<string>? ComboOptions { get; init; }
    public Func<string, string>? ComboDisplayName { get; init; }
}