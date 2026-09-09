using System.Threading;
using System.Threading.Tasks;

public interface IPlayFabService
{
    Task<bool> Initialize(IServiceRegistry serviceRegistry, CancellationToken ct);
}
