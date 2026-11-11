using System;
using System.Threading;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

[CreateAssetMenu(fileName = "CheckUpdateStep", menuName = "Startup/CheckUpdateStep")]
public class CheckUpdateStep : StartupStep
{
    public override bool HasTimeout => true;
    public override bool RequiresNetwork => true;
    public override bool IsMainThread => true;

    public override async Task<StartupStepResult> RunTasks(IServiceRegistry serviceRegistry, CancellationToken ct)
    {
        Debug.Log($"[CheckUpdateStep] Checking for updates...");

        var (isSuccess, serverVersion, errorMessage) = await CheckGameVersionAsync(ct);
        if (!isSuccess) return StartupStepResult.Failure("NETWORK_ERROR", errorMessage ?? "Failed to fetch version from server.");
        
        string localVersion = Application.version;

        if (IsVersionOutdated(localVersion, serverVersion))
        {
            Debug.LogWarning($"[CheckUpdateStep] Game is outdated! Local: {localVersion}, Server: {serverVersion}");
            return StartupStepResult.Failure("GAME_OUTDATED", $"Game version: {localVersion} is outdated. Required version: {serverVersion}");
        }
        
        Debug.Log($"[CheckUpdateStep] Game is up to date. Local: {localVersion}, Server: {serverVersion}");
        return StartupStepResult.Success();
    }

    private async Task<(bool isSuccess, string serverVersion, string error)> CheckGameVersionAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<(bool, string, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => tcs.TrySetCanceled());
        
        Debug.Log($"[CheckUpdateStep] Sending GetTitleData request.");
        
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), result =>
        {
            if (result.Data != null && result.Data.TryGetValue("Game_CurrentVersion", out string serverVersion))
            {
                tcs.TrySetResult((true, serverVersion, null));
            }
            else
            {
                tcs.TrySetResult((false, null, "Key 'Game_CurrentVersion' is missing in PlayFab TitleData.'"));
            }
        },
        error =>
        {
            Debug.LogError($"[CheckUpdateStep] Failed to get TitleData: " + error.GenerateErrorReport());
            tcs.TrySetResult((false, null, error.GenerateErrorReport()));
        });

        try
        {
            return await tcs.Task;
        }
        catch (TaskCanceledException)
        {
            return (false, null, "Check update task was canceled.");
        }
    }

    private bool IsVersionOutdated(string localVersion, string serverVersion)
    {
        if (Version.TryParse(localVersion, out Version localVer) &&
            Version.TryParse(serverVersion, out Version serverVer))
        {
            return localVer < serverVer;
        }
        
        return localVersion != serverVersion;
    }
}