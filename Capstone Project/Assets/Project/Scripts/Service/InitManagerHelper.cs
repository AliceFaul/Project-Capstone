using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InitManagerHelper : MonoBehaviour
{
    [SerializeField] private List<MonoBehaviour> managers = new();
    [SerializeField] private bool loadAssets = false;
    [SerializeField] public List<string> preloadGroups = new();
    [SerializeField] public string loadSceneWhenDone;
    
    private SceneLoader _sceneLoader;

    public async void Load()
    {
        try
        {
            _sceneLoader = SceneLoader.Instance;
            int totalTask = managers.Count;
            int taskDone = 0;
            
            // Validate
            foreach (var manager in managers)
            {
                if (manager is not IManager)
                {
                    throw new InvalidOperationException($"[InitManagerHelper] {manager.GetType().Name} not implement IManager!");
                }
            }

            foreach (var manager in managers)
            {
                var initManager = (IManager)manager;
                await initManager.Initialize();

                if (loadAssets && manager is ResourceManager)
                {
                    await LoadAssets();
                }
                
                taskDone++;
            }

            if(!loadAssets && taskDone == totalTask)
                await LoadScene();
        }
        catch (Exception e)
        {
            Debug.LogError($"[InitManagerHelper] Load error: {e.Message}\n{e.StackTrace}");
        }
    }
    
    private async Task LoadAssets()
    {
        int totalPreload = preloadGroups.Count;
        int preloadDone = 0;

        foreach (var group in preloadGroups)
        {
            var resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                throw new InvalidOperationException($"[InitManagerHelper] ResourceManager instance is null!");
            
            await resourceManager.Preload(group);
            preloadDone++;
        }

        if (preloadDone == totalPreload)
        {
            Debug.Log($"[InitManagerHelper] All asset preloaded successfully!");
            await LoadScene();
        }
    }
    
    private async Task LoadScene()
    {
        if (string.IsNullOrEmpty(loadSceneWhenDone))
        {
            Debug.LogError($"[InitManagerHelper] loadSceneWhenDone is null or empty, can't load scene!");
            return;
        }
        
        if(loadSceneWhenDone == "Loading")
            return;
        
        await _sceneLoader.LoadScene(loadSceneWhenDone, false);
    }
}