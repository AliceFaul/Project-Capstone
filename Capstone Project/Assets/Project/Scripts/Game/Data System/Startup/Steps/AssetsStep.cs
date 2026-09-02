using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetsStep", menuName = "Startup/AssetsStep")]
public class AssetsStep : StartupStep
{
    public override bool HasTimeout => true;

    public override async Task<StartupStepResult> RunTasks(IServiceRegistry serviceRegistry, CancellationToken ct)
    {
        ResourceManager rm = ResourceManager.Instance;
        
        if(rm == null)
            Debug.LogError("[AssetsStep] ResourceManager instance not found");
        
        try
        {
            await rm.Preload("Startup");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return StartupStepResult.Failure("ASSET_PRELOAD_FAILED", "Error while preloading assets");
        }

        return StartupStepResult.Success();
    }
}