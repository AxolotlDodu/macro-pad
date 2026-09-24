namespace MacroPad.Host.Integrations;

public class IntegrationRegistry
{
    private readonly List<IIntegration> _integrations;

    public IntegrationRegistry(IEnumerable<IIntegration> integrations) => _integrations = integrations.ToList();

    public async Task InitializeAllAsync()
    {
        foreach (var integration in _integrations)
        {
            try { await integration.InitializeAsync(); }
            catch (Exception ex) { Console.WriteLine($"[Integration:{integration.Key}] Échec d'initialisation : {ex.Message}"); }
        }
    }

    public T? Get<T>() where T : class, IIntegration => _integrations.OfType<T>().FirstOrDefault();

    public IAction? CreateAction(string type, BindingConfig config, IntegrationContext context)
    {
        foreach (var integration in _integrations)
        {
            if (!integration.IsAvailable) continue;
            var action = integration.CreateAction(type, config, context);
            if (action is not null) return action;
        }
        return null;
    }

    public IEncoderAction? CreateEncoderAction(string type, EncoderConfig config, IntegrationContext context)
    {
        foreach (var integration in _integrations)
        {
            if (!integration.IsAvailable) continue;
            var action = integration.CreateEncoderAction(type, config, context);
            if (action is not null) return action;
        }
        return null;
    }
}