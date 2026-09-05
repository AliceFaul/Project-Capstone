using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "ConfigStep", menuName = "Startup/ConfigStep")]
public class ConfigStep : StartupStep
{
    public override bool HasTimeout => true;
    
    public override async Task<StartupStepResult> RunTasks(IServiceRegistry serviceRegistry, CancellationToken ct)
    {
        IServiceRegistry sr = new ServiceRegistry();
        ConfigReferenceList configList = ResourceManager.Instance.GetAsset<ConfigReferenceList>("ConfigReferenceList");

        if (configList == null)
        {
            Debug.LogError($"[ConfigStep] Not found ConfigReferenceList asset. Please check assets has preloaded before config step.");
            return StartupStepResult.Failure("CONFIG_LIST_NOT_FOUND", $"ConfigReferenceList not found - please check again.");
        }
        
        IConfigLoader configLoader = new ConfigLoader();

        foreach (var config in configList.Configs)
        {
            var type = config.GetType();

            string fileName = type.Name + ".json";
            Debug.Log($"[ConfigStep] Reading {fileName}...");

            try
            {
                configLoader.LoadConfig(config);

                sr.Register(type, config);
                Debug.Log($"[ConfigStep] Read {fileName} successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigStep] Read {fileName} failed \n Exception: {e.Message}");
                return StartupStepResult.Failure("CONFIG_LOAD_FAILED", $"Failed to load configure: {fileName}");
            }
        }

        var configManager = new ConfigManager(sr);
        await configManager.Initialize(serviceRegistry, ct);
        
        return StartupStepResult.Success();
    }
}