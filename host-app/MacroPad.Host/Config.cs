using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MacroPad.Host;

public class BindingConfig
{
    public string Type { get; set; } = "";
    public string Target { get; set; } = "";
    public string Method { get; set; } = "GET";
    public string? Body { get; set; }
    public List<string>? Devices { get; set; } // pour sonar-cycle-output / sonar-cycle-mic
}

public class EncoderConfig
{
    /// <summary>Type d'action encodeur, ex: "sonar-volume".</summary>
    public string Type { get; set; } = "";
    /// <summary>Cible, ex: "master", "game", "chatRender".</summary>
    public string Target { get; set; } = "";
    /// <summary>Pas de volume par cran (0.0 - 1.0). Défaut 0.05 = 5%.</summary>
    public double Step { get; set; } = 0.05;
}

/// <summary>
/// Implémente INotifyPropertyChanged sur Name uniquement : ça permet au ListBox de pages
/// de la fenêtre de config de refléter un renommage en direct via un simple binding,
/// sans jamais avoir à remplacer l'élément dans l'ObservableCollection (ce qui plantait
/// le modèle de sélection d'Avalonia si la page renommée était celle sélectionnée).
/// </summary>
public class PageConfig : INotifyPropertyChanged
{
    private string _name = "Default";
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }
    }

    /// <summary>Sous-chaînes de nom de processus (insensible à la casse) qui déclenchent
    /// automatiquement le passage sur cette page quand la fenêtre correspondante prend le focus.</summary>
    public List<string> AppMatchers { get; set; } = new();

    public Dictionary<string, BindingConfig> Bindings { get; set; } = new();

    /// <summary>Clés "1" et "2" pour les deux encodeurs rotatifs.</summary>
    public Dictionary<string, EncoderConfig> Encoders { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class Config
{
    public List<PageConfig> Pages { get; set; } = new();

    /// <summary>Client ID de l'application Discord (portail développeur), pour le RPC local.</summary>
    public string? DiscordClientId { get; set; }

    /// <summary>Client Secret associé — nécessaire pour l'échange de code OAuth2.</summary>
    public string? DiscordClientSecret { get; set; }

    /// <summary>Ancien format plat (une seule page implicite). Conservé uniquement pour
    /// permettre la migration automatique ; jamais réécrit sur disque.</summary>
    [JsonPropertyName("bindings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, BindingConfig>? LegacyBindings { get; set; }

    public static Config Load(string path)
    {
        if (!File.Exists(path)) return NewDefault();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(path), options) ?? NewDefault();

        // Migration : ancien format plat "bindings" -> une page unique "Default"
        if ((config.Pages is null || config.Pages.Count == 0) && config.LegacyBindings is { Count: > 0 })
        {
            Console.WriteLine("[Config] Ancien format détecté, migration vers une page \"Default\".");
            config.Pages = new List<PageConfig>
            {
                new PageConfig { Name = "Default", Bindings = config.LegacyBindings }
            };
        }

        if (config.Pages is null || config.Pages.Count == 0)
            config.Pages = new List<PageConfig> { new PageConfig { Name = "Default" } };

        config.LegacyBindings = null;

        // Secrets Discord chargés depuis un fichier séparé, non versionné.
        var secretsPath = Path.Combine(Path.GetDirectoryName(path)!, "secrets.json");
        if (File.Exists(secretsPath))
        {
            var secrets = JsonSerializer.Deserialize<Config>(File.ReadAllText(secretsPath), options);
            if (secrets is not null)
            {
                config.DiscordClientId = secrets.DiscordClientId ?? config.DiscordClientId;
                config.DiscordClientSecret = secrets.DiscordClientSecret ?? config.DiscordClientSecret;
            }
        }

        return config;
    }

    public void Save(string path)
    {
        LegacyBindings = null; // on ne réécrit jamais l'ancien format
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(this, options));
    }

    private static Config NewDefault() => new Config
    {
        Pages = new List<PageConfig> { new PageConfig { Name = "Default" } }
    };
}