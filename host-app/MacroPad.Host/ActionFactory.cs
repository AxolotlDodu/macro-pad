namespace MacroPad.Host;

public static class ActionFactory
{
    public static IAction? Create(BindingConfig config, string? sonarAddress, IPageSwitcher? pageSwitcher = null, DiscordRpcClient? discordClient = null)
    {
        return config.Type switch
        {
            "launch" => new LaunchAction(config.Target),
            "url" => new UrlAction(config.Target),
            "keystroke" => new KeystrokeAction(config.Target),
            "http" => new HttpAction(config.Method, config.Target, config.Body),
            "sonar-toggle-mute" => sonarAddress is not null
                ? new SonarToggleMuteAction(sonarAddress, config.Target)
                : null,
            "switch-page" => pageSwitcher is not null
                ? new SwitchPageAction(pageSwitcher, config.Target)
                : null,
            "discord-toggle-mute" => discordClient is not null
                ? new DiscordMuteToggleAction(discordClient)
                : null,
            "discord-toggle-deafen" => discordClient is not null
                ? new DiscordDeafenToggleAction(discordClient)
                : null,
            _ => null
        };
    }

    public static IEncoderAction? CreateEncoder(EncoderConfig config, string? sonarAddress)
    {
        return config.Type switch
        {
            "sonar-volume" => sonarAddress is not null
                ? new SonarVolumeEncoderAction(sonarAddress, config.Target, config.Step > 0 ? config.Step : 0.05)
                : null,
            _ => null
        };
    }
}
