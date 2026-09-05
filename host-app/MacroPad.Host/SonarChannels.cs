namespace MacroPad.Host;

public static class SonarChannels
{
    public static readonly string[] All = { "master", "game", "chatRender", "media", "aux", "chatCapture" };

    private static readonly Dictionary<string, string> DisplayNames = new()
    {
        ["master"] = "Général",
        ["game"] = "Jeu",
        ["chatRender"] = "Chat",
        ["media"] = "Média",
        ["aux"] = "Aux",
        ["chatCapture"] = "Micro"
    };

    public static string GetDisplayName(string channel) =>
        DisplayNames.TryGetValue(channel, out var name) ? name : channel;
}