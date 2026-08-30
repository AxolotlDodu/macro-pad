using System.Text.Json;
using System.Text.Json.Serialization;

namespace MacroPad.Host;

public class BindingConfig
{
    public string Type { get; set; } = "";
    public string Target { get; set; } = "";
    public string Method { get; set; } = "GET";
    public string? Body { get; set; }
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

public class PageConfig
{
    public string Name { get; set; } = "Default";

    /// <summary>Sous-chaînes de nom de processus (insensible à la casse) qui déclenchent
    /// automatiquement le passage sur cette page quand la fenêtre correspondante prend le focus.</summary>
    public List<string> AppMatchers { get; set; } = new();

    public Dictionary<string, BindingConfig> Bindings { get; set; } = new();

    /// <summary>Clés "1" et "2" pour les deux encodeurs rotatifs.</summary>
    public Dictionary<string, EncoderConfig> Encoders { get; set; } = new();
}

public class Config
{
    public List<PageConfig> Pages { get; set; } = new();

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
