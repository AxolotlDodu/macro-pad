public class BindingConfig
{
    public string Type { get; set; } = "";
    public string Target { get; set; } = "";
}

public class Config
{
    public Dictionary<string, BindingConfig> Bindings { get; set; } = new();
}