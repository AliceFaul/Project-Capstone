using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SaveService : IGameService, IDisposable
{
    private const string SaveKey = "PLAYER_DATA";
    private const float AutoSaveIntervalMinutes = 5f; // Auto save per 5 minutes
    
    private readonly ConfigManager _configManager;
    private readonly PlayFabServiceManager _playFabService;
    private readonly JsonWriter _writer;
    private readonly JsonReader _reader;

    private PlayFabDataFlow _dataService;
    private PlayerDataConfig _config;

    private CancellationTokenSource _autoSaveCts;
    private bool _isCloudAvailable = true;
    
    public static event Action<string> OnSaveLoadErrorOccurred;
    public static event Action OnSaveCompletedSuccessfully;

    public SaveService(ConfigManager configManager, PlayFabServiceManager playFabService)
    {
        _configManager = configManager;
        _playFabService = playFabService;
        _writer = new JsonWriter(Application.persistentDataPath);
        _reader = new JsonReader(Application.persistentDataPath);
    }
    
    public async Task<bool> Initialize(IServiceRegistry serviceRegistry, CancellationToken ct = default)
    {
        serviceRegistry.Register<SaveService>(this);
        
        if (!_configManager.GetConfig(out PlayerDataConfig config))
        {
            Debug.LogError($"[SaveService] Not found player data config in Config Manager.");
            return false;
        }
        _config = config;

        if (!_playFabService.TryGetService(out PlayFabDataFlow dataService))
        {
            Debug.LogError($"[SaveService] Not found data service in PlayFab services list.");
            return false;
        }
        _dataService = dataService;
        
        EventManager.Instance.AddListener("ON_AUTH_SUCCESS", OnAuthSuccess);
        
        await Task.CompletedTask;
        return true;
    }

    private async void OnAuthSuccess()
    {
        try
        {
            await LoadData();
            StartAutoSaveLoop();
        }
        catch(Exception e) { Debug.LogError($"[SaveService] Failed to load data. Error: {e.Message}"); }
    }

    private void StartAutoSaveLoop()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts = new CancellationTokenSource();
        _ = AutoSaveLoop(_autoSaveCts.Token);
    }

    private async Task LoadData(CancellationToken ct = default)
    {
        string cloudJson = null;
        _isCloudAvailable = true;

        try
        {
            cloudJson = await _dataService.Load(SaveKey, ct);
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[SaveService] Loading data timeout/canceled. Use local data.");
        }
        catch (Exception ex)
        {
            _isCloudAvailable = false;
            OnSaveLoadErrorOccurred?.Invoke("Unable to connect to the PlayFab server. Using offline data.");
            Debug.LogError($"[SaveService] Cloud load connection error: {ex.Message}");
        }

        if (!string.IsNullOrEmpty(cloudJson))
        {
            try
            {
                var gameData = JsonUtility.FromJson<GameData>(cloudJson);
                _config.ApplyGameData(gameData);
                Debug.Log($"[SaveService] Charged data from PlayFab (cloud) successfully.");
                _writer.Write(gameData, _config.name + ".json");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Parse cloud data failed: {e.Message}");
                OnSaveLoadErrorOccurred?.Invoke("The cloud data has a structural error.");
            }
        }
        else
        {
            Debug.Log($"[SaveService] Cloud data unavailable/empty. Reading local cache...");
            LocalBackup();
        }
    }

    public async Task<bool> SaveData(CancellationToken ct = default)
    {
        if(_config == null || _dataService == null) return false;
        var currentData = _config.ToGameData();

        // ===== PRIORITY: LOCAL > CLOUD =====
        try
        {
            _writer.Write(currentData, _config.name + ".json");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveService] Failed to write local cache. Error: {e.Message}");
        }

        string json = JsonUtility.ToJson(currentData);
        bool cloudOk = false;
        
        try
        {
            cloudOk = await _dataService.Save(SaveKey, json, ct);
            if(cloudOk) _isCloudAvailable = true;
        }
        catch(Exception ex)
        {
            _isCloudAvailable = false;
            Debug.LogError($"[SaveService] Cloud save failed network error: {ex.Message}");
        }

        if (!cloudOk)
        {
            Debug.LogError($"[SaveService] Saving in cloud failed. Progress is cached locally.");
            OnSaveLoadErrorOccurred?.Invoke("Connection lost! The process is temporarily saved locally on your computer.");
        }
        
        return cloudOk;
    }

    private async Task AutoSaveLoop(CancellationToken ct = default)
    {
        while(!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(AutoSaveIntervalMinutes), ct);
                Debug.Log("$[SaveService] Triggering periodic auto-save...");
                bool success = await SaveData(ct);

                if(success) OnSaveCompletedSuccessfully?.Invoke();
            }
            catch(OperationCanceledException) { Debug.Log($"[SaveService] Auto-save loop stopped."); break; }
            catch(Exception e) { Debug.LogError($"[SaveService] Error in auto-save loop: {e.Message}"); }
        }
    }

    private void LocalBackup()
    {
        try
        {
            string localJson = _reader.Read(_config.name + ".json");
            if(!string.IsNullOrEmpty(localJson))
            {
                var localData = JsonUtility.FromJson<GameData>(localJson);
                _config.ApplyGameData(localData);
                Debug.Log($"[SaveService] Charged data from PlayFab (local) successfully.");
            }
            else
            {
                Debug.Log($"[SaveService] No local cache found. Starting with clean data config.");
            }
        }
        catch(Exception e)
        {
            Debug.LogError($"[SaveService] Parse local data failed: {e.Message}");
            OnSaveLoadErrorOccurred?.Invoke($"[SaveService] The local files stored on the machine are corrupted.");
        }
    }

    public void Dispose()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts?.Dispose();
        EventManager.Instance?.RemoveListener("ON_AUTH_SUCCESS", OnAuthSuccess);
    }
}
