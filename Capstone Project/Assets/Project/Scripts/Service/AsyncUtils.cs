using System.Threading;
using System.Threading.Tasks;

public static class AsyncUtils
{
    public static async Task WaitWithCancellation(Task task, CancellationToken ct)
    {
        if (!ct.CanBeCanceled)
        {
            await task;
            return;
        }
        
        var cancelTask = Task.Delay(Timeout.Infinite, ct);
        var completed = await Task.WhenAny(task, cancelTask);
        
        if(completed == cancelTask)
            ct.ThrowIfCancellationRequested();
        
        await task;
    }

    public static async Task<T> WaitWithCancellation<T>(Task<T> task, CancellationToken ct)
    {
        if (!ct.CanBeCanceled) 
            return await task;
        
        var cancelTask = Task.Delay(Timeout.Infinite, ct);
        var completed = await Task.WhenAny(task, cancelTask);
        
        if(completed == cancelTask)
            ct.ThrowIfCancellationRequested();
        
        return await task;
    }
}