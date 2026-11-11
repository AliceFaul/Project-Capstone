using System.Threading;
using System.Threading.Tasks;

public class PlayFabServiceManager : IGameService
{
    private readonly IServiceRegistry _serviceRegistry;

    public PlayFabServiceManager(IServiceRegistry serviceRegistry)
    {
        _serviceRegistry = serviceRegistry;
    }
    
    public async Task<bool> Initialize(IServiceRegistry serviceRegistry, CancellationToken ct = default)
    {
        serviceRegistry.Register<PlayFabServiceManager>(this);
        await Task.CompletedTask;
        return true;
    }

    public TService GetService<TService>()
    {
        return _serviceRegistry.Get<TService>();
    }

    public bool TryGetService<TService>(out TService service)
    {
        return _serviceRegistry.TryGet<TService>(out service);
    }
}