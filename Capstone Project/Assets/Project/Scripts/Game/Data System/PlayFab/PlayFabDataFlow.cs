using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;

[CreateAssetMenu(fileName = "PlayFabDataFlow", menuName = "PlayFab/Data Flow")]
public class PlayFabDataFlow : PlayFabService
{
    public override async Task<bool> Initialize(IServiceRegistry serviceRegistry, CancellationToken ct)
    {
        serviceRegistry.Register<PlayFabDataFlow>(this);
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> Save(string key, string json, CancellationToken ct)
    {
        if(ct.IsCancellationRequested) return false;
        var tcs = new TaskCompletionSource<bool>();

        await using(ct.Register(() => tcs.TrySetCanceled()))
        {
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { key, json } }
            };

            PlayFabClientAPI.UpdateUserData(request, result =>
            {
                Debug.Log($"[PlayFabDataFlow] Updated data with {key} successfully.");
                tcs.TrySetResult(true);
            },
            error =>
            {
                Debug.LogError($"[PlayFabDataFlow] Failed save {key}. Error: {error.GenerateErrorReport()}");
                tcs.TrySetResult(false);
            });

            try
            {
                return await AsyncUtils.WaitWithCancellation(tcs.Task, ct);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[PlayFabDataFlow] Cancelled operation {key}.");
                return false;
            }
        }
    }

    public async Task<string> Load(string key, CancellationToken ct)
    {
        if(ct.IsCancellationRequested) return null;
        var tcs = new TaskCompletionSource<string>();

        await using (ct.Register(() => tcs.TrySetCanceled()))
        {
            var request = new GetUserDataRequest
            {
                Keys = new List<string> { key }
            };

            PlayFabClientAPI.GetUserData(request, result =>
            {
                if (result.Data != null && result.Data.TryGetValue(key, out var record))
                {
                    Debug.Log($"[PlayFabDataFlow] Loaded data with {key} successfully.");
                    tcs.TrySetResult(record.Value);
                }
                else
                {
                    Debug.LogWarning(
                        $"[PlayFabDataFlow] Not found data with {key} (maybe your first time played).");
                    tcs.TrySetResult(null);
                }
            },
            error =>
            {
                Debug.LogError($"[PlayFabDataFlow] Read data with {key} failed. Error: {error.GenerateErrorReport()}");
                tcs.TrySetResult(null);  
            });

            try
            {
                return await AsyncUtils.WaitWithCancellation(tcs.Task, ct);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[PlayFabDataFlow] Canceled operation {key}.]");
                return null;
            }
        }
    }
}