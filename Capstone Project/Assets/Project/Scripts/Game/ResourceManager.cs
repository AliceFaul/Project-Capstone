using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

public class ResourceManager : MonoBehaviour, IManager
{
    public static ResourceManager Instance;
    private readonly Dictionary<string, AssetReferences> _assetReferences = new();
    private readonly Dictionary<string, AsyncOperationHandle> _loadedAssets = new();
    private readonly HashSet<string> _preloadedGroups = new();

    public async Task<bool> Initialize()
    {
        var handle = Addressables.LoadAssetAsync<AssetReferencesList>("AssetReferencesList");
        await handle.Task;
        var assetReferencesList = handle.Result;

        foreach (var assetReference in assetReferencesList.References)
        {
            _assetReferences.Add(assetReference.Key, assetReference);
        }
        return true;
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async Task Preload(string groupKey)
    {
        if (_assetReferences.TryGetValue(groupKey, out var assetReferences))
        {
            if (assetReferences.Assets.Count > 0)
            {
                var handles = assetReferences.Assets
                    .Select(ar => ar.LoadAssetAsync<Object>())
                    .ToList();
                
                var toLoad = assetReferences.Assets
                    .Where(ar => ar.Asset == null || !_loadedAssets.ContainsKey(ar.Asset.name))
                    .ToList();

                await Task.WhenAll(handles.Select(h => h.Task));

                for (int i = 0; i < handles.Count; i++)
                {
                    var handle = handles[i];
                    var assetRef = toLoad[i];
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    { 
                        _loadedAssets[assetRef.Asset.name] = handle;
                        Debug.Log($"[ResourceManager] {assetRef.Asset.name} has been loaded");
                    }
                    else
                    {
                        Debug.LogError($"[ResourceManager] failed to load asset in group {groupKey} (index {i})");
                    }
                }
            }

            if (assetReferences.Labels.Count > 0)
            {
                foreach (var label in assetReferences.Labels)
                {
                    var locations = await Addressables.LoadResourceLocationsAsync(label).Task;
                    
                    var locationsToLoad = locations
                        .Where(loc => !_loadedAssets.ContainsKey(loc.PrimaryKey)).ToList();
                    
                    var handles = locationsToLoad
                        .Select(Addressables.LoadAssetAsync<Object>).ToList();

                    await Task.WhenAll(handles.Select(h => h.Task));

                    for (int i = 0; i < handles.Count; i++)
                    {
                        var handle = handles[i];
                        var location = locations[i];
                        if (handle.Status == AsyncOperationStatus.Succeeded)
                        {
                            _loadedAssets[location.PrimaryKey] = handle;
                            Debug.Log($"[ResourceManager] {location.PrimaryKey} has been loaded");
                        }
                        else
                        {
                            Debug.LogError($"[ResourceManager] {location.PrimaryKey} hasn't been loaded");
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError($"[ResourceManager] Asset with key: {groupKey} load failed");
        }
        
        _preloadedGroups.Add(groupKey);
        Debug.Log($"[ResourceManager] {groupKey} has been preloaded");
    }

    public T GetAsset<T>(string assetKey) where T : Object
    {
        if (_loadedAssets.TryGetValue(assetKey, out AsyncOperationHandle handle))
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result as T;
            }
            else
            {
                Debug.LogError($"[ResourceManager] Asset with key: {assetKey} not loaded successfully");
            }
        }
        else
        {
            Debug.LogError($"[ResourceManager] Asset with key: {assetKey} not found in cache. Do you forget call Preload()?");
        }

        return null;
    }
    
    public bool IsLoaded(string assetKey) => _loadedAssets.ContainsKey(assetKey);

    public void Release(string key)
    {
        if (_loadedAssets.TryGetValue(key, out AsyncOperationHandle handle))
        {
            Addressables.Release(handle);
            _loadedAssets.Remove(key);
            Debug.Log($"[ResourceManager] {key} has been released");
        }
        else
        {
            Debug.LogError($"[ResourceManager] Asset with key: {key} not found in cache");
        }
    }

    public void ReleaseAssetReferences(string groupKey)
    {
        if (_assetReferences.TryGetValue(groupKey, out var assetReferences))
        {
            foreach (var assetReference in assetReferences.Assets)
            {
                if(assetReference.Asset == null)
                    return;
                
                var runtimeKey = assetReference.Asset.name;
                if (_loadedAssets.ContainsKey(runtimeKey))
                {
                    assetReference.ReleaseAsset();
                    _loadedAssets.Remove(runtimeKey);
                    Debug.Log($"[ResourceManager] {runtimeKey} has been released");
                }
            }

            foreach (var label in assetReferences.Labels)
            {
                var locations = Addressables.LoadResourceLocationsAsync(label).WaitForCompletion();
                foreach (var location in locations)
                {
                    string locationKey = location.PrimaryKey;
                    if (_loadedAssets.TryGetValue(locationKey, out var handle))
                    {
                        Addressables.Release(handle);
                        _loadedAssets.Remove(locationKey);
                        Debug.Log($"[ResourceManager] {locationKey} has been released");
                    }
                }
            }
        }
        else
        {
            Debug.LogError($"[ResourceManager] Asset with key: {groupKey} not found in cache");
        }
        
        _preloadedGroups.Remove(groupKey);
    }
    
    // Just for asset test/editor, not for published
    public static T LoadResource<T>(string path) where T : Object
    {
        return Resources.Load<T>(path);
    }
}