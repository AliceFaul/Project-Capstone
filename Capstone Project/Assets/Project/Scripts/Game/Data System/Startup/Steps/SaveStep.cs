using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "SaveStep", menuName = "Startup/SaveStep")]
public class SaveStep : StartupStep
{
    public override bool HasTimeout => true;
    public override bool RequiresNetwork => false;

    public override async Task<StartupStepResult> RunTasks(IServiceRegistry serviceRegistry, CancellationToken ct)
    {
        if (!serviceRegistry.TryGet<ConfigManager>(out var configMg))
        {
            Debug.LogError($"[SaveStep] Not found Config Manager - Please run config step first.");
            return StartupStepResult.Failure("CONFIG_MANAGER_NOT_FOUND", $"Save step require Config Manager to run first.");
        }

        if (!serviceRegistry.TryGet<PlayFabServiceManager>(out var playFabMg))
        {
            Debug.LogError($"[SaveStep] Not found PlayFab Service Manager - Please run PlayFab step first.");
            return StartupStepResult.Failure("PLAY_FAB_MANAGER_NOT_FOUND", $"Save step require PlayFab service manager to run first.");
        }

        var saveService = new SaveService(configMg, playFabMg);
        try
        {
            bool initService = await saveService.Initialize(serviceRegistry, ct);
            if (!initService)
            {
                Debug.LogError($"[SaveStep] Failed to initialize save service.");
                return StartupStepResult.Failure("SAVE_INIT_FAILED", $"Save service failed to initialize.");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[SaveStep] Initialize timeout or cancelled");
            return StartupStepResult.Failure("SAVE_INIT_TIMEOUT", $"Save service initialization timed out.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return StartupStepResult.Failure("SAVE_INIT_EXCEPTION",  $"Exception while initializing Save service: {e.Message}");
        }

        return StartupStepResult.Success();
    }
}