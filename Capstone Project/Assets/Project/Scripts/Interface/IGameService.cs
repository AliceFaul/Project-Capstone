using System.Threading;
using System.Threading.Tasks;

public interface IGameService
{
    Task<bool> Initialize(IServiceRegistry serviceRegistry, CancellationToken ct = default);
}