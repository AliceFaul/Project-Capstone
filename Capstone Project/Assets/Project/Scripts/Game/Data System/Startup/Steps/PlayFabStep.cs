using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayFabStep", menuName = "Startup/PlayFabStep")]
public class PlayFabStep : StartupStep
{
    public override bool HasTimeout => true;
    public override bool RequiresNetwork => true;
    public override bool IsMainThread => true;

    public override async Task<StartupStepResult> RunTasks(IServiceRegistry serviceRegistry, CancellationToken ct)
    {
        IServiceRegistry sr = new ServiceRegistry();
        var playFabServiceList = ResourceManager.Instance.GetAsset<PlayFabServiceList>("PlayFabServiceList");

        if (playFabServiceList == null)
        {
            Debug.LogError($"[PlayFabStep] Can't find PlayFabServiceList");
            return StartupStepResult.Failure("PLAY_FAB_LIST_NOT_FOUND", $"PlayFab service list not found. Please check again.");
        }

        foreach (var service in playFabServiceList.Services)
        {
            string serviceName = service.GetType().Name;
            Debug.Log($"[PlayFabStep] Initialize service {serviceName}");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));
            try
            {
                var initService = await service.Initialize(sr, timeoutCts.Token);
                if (!initService)
                {
                    Debug.LogError($"[PlayFabStep] Failed to initialize service {serviceName}]");
                    return StartupStepResult.Failure("PLAY_FAB_INIT_FAILED",
                        $"Failed to initialize service {serviceName}");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[PlayFabStep] Failed to initialize timeout or cancelled {serviceName}]");
                return StartupStepResult.Failure("PLAY_FAB_INIT_TIMEOUT",
                    $"PlayFab service initialization timed out: {serviceName}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return StartupStepResult.Failure("PLAY_FAB_INIT_EXCEPTION", $"Exception while initializing service {serviceName}");
            }
        }

        var playFabManager = new PlayFabServiceManager(sr);
        await playFabManager.Initialize(serviceRegistry, ct);
        
        var playFabAuthentication = playFabManager.GetService<PlayFabAuthentication>();
        bool isLoginSuccess = await playFabAuthentication.DefaultIdLogin();

        if (!isLoginSuccess)
            return StartupStepResult.Failure("PLAY_FAB_LOGIN_FAILED", $"Couldn't sign in");
        else 
            Debug.Log($"[PlayFabStep] Successfully logged in");

        return StartupStepResult.Success();
    }
}