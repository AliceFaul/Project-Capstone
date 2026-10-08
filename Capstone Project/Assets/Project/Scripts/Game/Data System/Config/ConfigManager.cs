using System.Threading;
using System.Threading.Tasks;

public class ConfigManager : IGameService
{
    private readonly IServiceRegistry _serviceRegistry;

    public ConfigManager(IServiceRegistry serviceRegistry)
    {
        _serviceRegistry = serviceRegistry;
    }
    
    public async Task<bool> Initialize(IServiceRegistry serviceRegistry, CancellationToken ct = default)
    {
        serviceRegistry.Register<ConfigManager>(this);
        await Task.CompletedTask;
        return true;
    }

    public TService GetConfig<TService>()
    {
        return _serviceRegistry.Get<TService>();
    }

    public bool GetConfig<TService>(out TService service)
    {
        return _serviceRegistry.TryGet<TService>(out service);
    }
}