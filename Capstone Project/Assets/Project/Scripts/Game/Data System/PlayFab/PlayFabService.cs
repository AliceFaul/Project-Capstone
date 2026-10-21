using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class PlayFabService : ScriptableObject, IPlayFabService
{
    public abstract Task<bool> Initialize(IServiceRegistry serviceRegistry, CancellationToken ct);
}