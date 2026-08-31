using System.Threading;
using UnityEngine;
using System.Threading.Tasks;

public struct StartupStepResult
{
    public bool IsSuccess;
    public string ErrorId;
    public string Message;
    
    public static StartupStepResult Success() => new StartupStepResult { IsSuccess = true };
    public static StartupStepResult Failure(string errorId, string message) => new StartupStepResult { IsSuccess = false, ErrorId = errorId, Message = message };
}

public abstract class StartupStep : ScriptableObject
{
    public abstract bool HasTimeout { get; }
    public virtual bool RequiresNetwork => false;
    public virtual bool IsMainThread => false;

    public abstract Task<StartupStepResult> RunTasks(IServiceRegistry serviceRegistry, CancellationToken ct);
}