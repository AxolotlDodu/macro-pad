using System.Text.Json;
using MacroPad.Soundboard;

namespace MacroPad.Host.Integrations;

public class SoundboardIntegration : IIntegration
{
    public string Key => "soundboard";
    public bool IsAvailable => true; // échec silencieux géré par SoundboardClient si l'app n'est pas lancée

    private readonly SoundboardClient _client;

    public IReadOnlyList<ActionTypeDescriptor> ButtonActionTypes { get; }
    public IReadOnlyList<ActionTypeDescriptor> EncoderActionTypes { get; } = Array.Empty<ActionTypeDescriptor>();

    public SoundboardIntegration()
    {
        var soundboardDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Soundboard");

        _client = new SoundboardClient(Path.Combine(soundboardDir, "port.txt"));
        var sounds = LoadSounds(Path.Combine(soundboardDir, "sounds.json"));

        ButtonActionTypes = new List<ActionTypeDescriptor>
        {
            new()
            {
                Id = "soundboard-play",
                Label = "Soundboard : Jouer un son",
                Target = TargetKind.ChannelCombo,
                ComboOptions = sounds.Keys.ToList(),
                ComboDisplayName = id => sounds.GetValueOrDefault(id, id)
            }
        };
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public IAction? CreateAction(string type, BindingConfig config, IntegrationContext context) => type switch
    {
        "soundboard-play" => new SoundboardPlayAction(_client, config.Target),
        _ => null
    };

    public IEncoderAction? CreateEncoderAction(string type, EncoderConfig config, IntegrationContext context) => null;

    private static Dictionary<string, string> LoadSounds(string path)
    {
        var result = new Dictionary<string, string>();
        if (!File.Exists(path)) return result;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                var id = entry.GetProperty("Id").GetString();
                var displayName = entry.TryGetProperty("DisplayName", out var dn) ? dn.GetString() : id;
                if (id is not null) result[id] = displayName ?? id;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Integration:soundboard] Échec lecture sounds.json : {ex.Message}");
        }

        return result;
    }
}