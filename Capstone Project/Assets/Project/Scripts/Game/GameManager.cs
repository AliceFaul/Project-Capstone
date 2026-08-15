using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Managers")]
    [SerializeField] private List<MonoBehaviour> managers = new();
    
    [Header("Preload Assets")]
    [SerializeField] private string[] preloadGroups;
    [SerializeField] private bool preloadGroupsInParallel;
    
    [Header("Loading Screen")]
    [SerializeField] private MonoBehaviour loadingScreen;
    
    [Header("Debug")]
    [SerializeField] private bool verboseLogging;

    private ILoading _loadingScreen; 
        
    public event Action OnGameReady;
    public bool IsReady { get; private set; }

    private void OnValidate()
    {
        foreach (var manager in managers.Where(manager => manager != null && manager is not IManager))
        {
            Debug.LogError($"[GameManager] {manager.name} isn't implement IManager", manager);
        }

        if (loadingScreen != null && loadingScreen is not ILoading)
        {
            Debug.LogError($"[GameManager] {loadingScreen.name} isn't implement ILoading", loadingScreen);
        }
    }

    private async void Awake()
    {
        try
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _loadingScreen = loadingScreen as ILoading;
            _loadingScreen?.Show();

            await BootSequence();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] {gameObject.name} failed to initialize!");
        }
    }

    private async Task BootSequence()
    {
        var managerList = managers.OfType<IManager>().ToList();
        int totalSteps = managerList.Count + (preloadGroups?.Length ?? 0);
        int completedSteps = 0;
        
        // Step 1: Initialize all IManager in List
        foreach (var manager in managerList)
        {
            string managerName = (manager as MonoBehaviour)?.GetType().Name ?? manager.GetType().Name;
            if (verboseLogging)
            {
                Debug.Log($"[GameManager] Initializing {managerName}...");
            }

            bool success;
            try
            {
                success = await manager.Initialize();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] {managerName}.Initialize() failed: {e.Message}");
                success = false;
            }

            if (!success)
            {
                Debug.LogError($"[GameManager] {managerName} failed to initialize!");
                return;
            }
            
            completedSteps++;

            if (verboseLogging)
            {
                Debug.Log($"[GameManager] {managerName} initialized successfully!");
            }
        }
        
        // Step 2: Preload asset groups
        if (preloadGroups is { Length: > 0 })
        {
            if (ResourceManager.Instance == null)
            {
                Debug.LogError($"[GameManager] Check ResourceManager in managers and initialized!");
            } 
            else if (preloadGroupsInParallel)
            {
                var tasks = preloadGroups.Select(g => ResourceManager.Instance.Preload(g)).ToArray();
                await Task.WhenAll(tasks);
                
                completedSteps += preloadGroups.Length;
            }
            else
            {
                foreach (var group in preloadGroups)
                {
                    if (verboseLogging)
                    {
                        Debug.Log($"[GameManager] Preloading group {group}...");
                    }
                    await ResourceManager.Instance.Preload(group);
                    completedSteps++;
                }
            }
        }
        
        // Step 3: Finished. Disable loading screen and invoke event
        _loadingScreen?.Hide();
        
        IsReady = true;
        OnGameReady?.Invoke();

        if (verboseLogging)
        {
            Debug.Log($"[GameManager] Finished boot sequence. Game is ready to started.");
        }
    }
}
