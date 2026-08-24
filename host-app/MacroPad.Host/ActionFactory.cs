public static class ActionFactory
{
    public static IAction? Create(BindingConfig config)
    {
        return config.Type switch
        {
            "launch" => new LaunchAction(config.Target),
            "url" => new UrlAction(config.Target),
            _ => null
        };
    }
}