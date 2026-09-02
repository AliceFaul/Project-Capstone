using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "InitManagersStep", menuName = "Startup/InitManagersStep")]
public class InitManagersStep : StartupStep
{
    [SerializeField] private string rootManager;
    
#if UNITY_EDITOR
    [SerializeField] private List<MonoScript> monoScripts;  
#endif
    
    [SerializeField, HideInInspector] private List<string> managerNames = new List<string>();
    
    public override bool HasTimeout => true;
    
    public override async Task<StartupStepResult> RunTasks(IServiceRegistry serviceRegistry, CancellationToken ct)
    {
        var rootManagerTransform = GameObject.Find(rootManager)?.transform;
        
        if (rootManagerTransform == null)
        {
            Debug.LogError($"[InitManagersStep] Can't find root manager {rootManager}");
            return StartupStepResult.Failure("ROOT_MANAGER_NOT_FOUND", "Can't find root manager");
        }

        foreach (var typeName in managerNames)
        {
            var type = Type.GetType(typeName);

            if (type == null)
            {
                Debug.LogError($"[InitManagersStep] Can't find type {typeName}");
                return StartupStepResult.Failure("MANAGER_TYPE_NOT_FOUND", $"Can't resolve type {typeName}");
            }
            
            var component = rootManagerTransform.GetComponent(type);
            if (component == null)
                component = rootManagerTransform.gameObject.AddComponent(type);
            
            IManager manager = (IManager)component;
            await manager.Initialize();
            serviceRegistry.Register(type, manager);
        }

        return StartupStepResult.Success();
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        List<MonoScript> removedScripts = new List<MonoScript>();
        managerNames.Clear();

        foreach (var script in monoScripts)
        {
            var type = script.GetClass();
            if (!typeof(IManager).IsAssignableFrom(type))
            {
                Debug.LogWarning($"[InitManagersStep] {script.name} must implement IManager!");
                removedScripts.Add(script);
            }
            else
            {
                managerNames.Add(type.AssemblyQualifiedName);
            }
        }
        
        foreach(var script in removedScripts)
            monoScripts.Remove(script);
    }
#endif
}