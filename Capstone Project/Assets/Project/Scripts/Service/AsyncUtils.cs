using System;
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
        if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        using (ct.Register(() => tcs.TrySetCanceled(ct)))
        {
            Task completedTask = await Task.WhenAny(task, tcs.Task);
            if(completedTask == task) return await task;
            throw new OperationCanceledException(ct);
        }
    }
}