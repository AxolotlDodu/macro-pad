using System.Text.Json;
using MacroPad.Soundboard;

namespace MacroPad.Host.Integrations;

public class SoundboardIntegration : IIntegration
{
    public string Key => "soundboard";
    public bool IsAvailable => true;

    private readonly SoundboardClient _client;
    private readonly string _soundsPath;

    public IReadOnlyList<ActionTypeDescriptor> ButtonActionTypes { get; private set; }
    public IReadOnlyList<ActionTypeDescriptor> EncoderActionTypes { get; } = new List<ActionTypeDescriptor>
    {
        new() { Id = "soundboard-volume", Label = "Volume", Target = TargetKind.None, IntegrationKey = "soundboard" },
    };

    public SoundboardIntegration()
    {
        var soundboardDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Soundboard");

        _client = new SoundboardClient(Path.Combine(soundboardDir, "port.txt"));
        _soundsPath = Path.Combine(soundboardDir, "sounds.json");

        ButtonActionTypes = BuildButtonActionTypes();
    }

    /// <summary>Relit sounds.json. Appelé par ConfigWindow avant chaque ouverture d'un éditeur
    /// de touche/encodeur pour que la liste des sons proposée soit toujours à jour.</summary>
    public void RefreshSounds() => ButtonActionTypes = BuildButtonActionTypes();

    private List<ActionTypeDescriptor> BuildButtonActionTypes()
    {
        var sounds = LoadSounds(_soundsPath);
        return new List<ActionTypeDescriptor>
        {
            new()
            {
                Id = "soundboard-play",
                Label = "Jouer un son",
                Target = TargetKind.ChannelCombo,
                ComboOptions = sounds.Keys.ToList(),
                ComboDisplayName = id => id is null ? "" : sounds.GetValueOrDefault(id, id),
                IntegrationKey = "soundboard"
            },
            new()
            {
                Id = "soundboard-toggle-mute",
                Label = "Mute/Unmute",
                Target = TargetKind.None,
                IntegrationKey = "soundboard"
            },
            new()
            {
                Id = "soundboard-stop-all",
                Label = "Soundboard : Tout arrêter",
                Target = TargetKind.None,
                IntegrationKey = "soundboard"
            },
        };
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public IAction? CreateAction(string type, BindingConfig config, IntegrationContext context) => type switch
    {
        "soundboard-play" => new SoundboardPlayAction(_client, config.Target),
        "soundboard-toggle-mute" => new SoundboardMuteToggleAction(_client, context.Notifier),
        "soundboard-stop-all" => new SoundboardStopAllAction(_client),
        _ => null
    };

    public IEncoderAction? CreateEncoderAction(string type, EncoderConfig config, IntegrationContext context) => type switch
    {
        "soundboard-volume" => new SoundboardVolumeEncoderAction(
            _client,
            config.Step > 0 ? (int)Math.Round(config.Step * 100) : 2,
            context.Notifier),
        _ => null
    };

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