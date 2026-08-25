using SharpHook.Data;
using SharpHook.Simulation;

public class KeystrokeAction : IAction, IDisposable
{
    private readonly KeyCode[] _keys;
    private readonly EventSimulator _simulator = EventSimulator.Create("MacroPad.Host");

    public KeystrokeAction(string target)
    {
        _keys = target.Split('+', StringSplitOptions.TrimEntries)
                       .Select(ParseKey)
                       .ToArray();
    }

    public void Execute()
    {
        _simulator.SimulateKeyStroke(_keys);
    }

    public void Dispose() => _simulator.Dispose();

    private static KeyCode ParseKey(string name) => name.ToLowerInvariant() switch
    {
        "ctrl" or "control" => KeyCode.VcLeftControl,
        "shift" => KeyCode.VcLeftShift,
        "alt" => KeyCode.VcLeftAlt,
        "win" or "super" or "meta" => KeyCode.VcLeftMeta,
        "esc" or "escape" => KeyCode.VcEscape,
        "tab" => KeyCode.VcTab,
        "enter" or "return" => KeyCode.VcEnter,
        "space" => KeyCode.VcSpace,
        "delete" or "del" => KeyCode.VcDelete,
        _ when System.Text.RegularExpressions.Regex.IsMatch(name, @"^f([1-9]|1[0-9]|2[0-4])$")
            => Enum.Parse<KeyCode>($"Vc{name.ToUpperInvariant()}"),
        _ when name.Length == 1 => Enum.Parse<KeyCode>($"Vc{char.ToUpperInvariant(name[0])}"),
        _ => throw new ArgumentException($"Touche inconnue : {name}")
    };
}