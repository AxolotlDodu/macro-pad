public static class ActionFactory
{
    public static IAction? Create(BindingConfig config, string? sonarAddress)
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
            _ => null
        };
    }
}