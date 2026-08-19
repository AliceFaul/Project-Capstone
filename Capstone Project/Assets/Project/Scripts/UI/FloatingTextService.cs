using UnityEngine;
using UnityEngine.Localization;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FloatingTextService : IFloatingTextService
{
    private readonly Dictionary<string, GameObject> _textPrefabs = new();
    private readonly Dictionary<string, GameObject> _activeTexts = new();

    private GameObject _canvas;

    public FloatingTextService()
    {
        FloatingTextContainer floatingText = ResourceManager.Instance.GetAsset<FloatingTextContainer>("FloatingTextContainer");

        if (floatingText == null)
        {
            Debug.LogError($"[FloatingTextService] Floating Text Container not found! Need to preload in Resource Manager");
            return;
        }
        
        foreach (var entry in floatingText.FloatingTexts)
        {
            _textPrefabs[entry.id] = entry.prefab;
        }
    }

    public void Create(string prefabId, string instanceId, string content, Vector3 position, bool isMoving)
    {
        if (_activeTexts.ContainsKey(instanceId))
        {
            Destroy(instanceId, 0f);
        }

        if (!_textPrefabs.TryGetValue(prefabId, out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[FloatingTextService] Floating text with prefab id {prefabId} not found]");
            return;
        }

        if (_canvas == null)
        {
            CanvasCreator canvasCreator = new CanvasCreator();
            _canvas = canvasCreator.Create(true);
        }
        
        GameObject textGo = GameObject.Instantiate(prefab, _canvas.transform);
        textGo.transform.position = position;

        if (textGo.GetComponent<FloatingText>() == null)
        {
            Debug.LogError($"[FloatingTextService] Prefab {prefabId} missed FloatingText component. Destroy!");
            GameObject.Destroy(textGo);
            return;
        }

        textGo.GetComponent<FloatingText>().Setup(instanceId, content, isMoving, (id) =>
        {
            if (_activeTexts.TryGetValue(id, out GameObject instance))
            {
                if (instance != null)
                {
                    GameObject.Destroy(instance.gameObject);
                }
                _activeTexts.Remove(id);
            }
        });
        
        _activeTexts.Add(instanceId, textGo);
    }
    
    public void Create(string prefabId, string instanceId, LocalizedString content, Vector3 position, bool isMoving)
    {
        content.GetLocalizedStringAsync().Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Create(prefabId, instanceId, handle.Result, position, isMoving);
            }
            else
            {
                Debug.LogError($"[FloatingTextService] Can't resolve LocalizedString for instance id {instanceId}.");
            }
        };
    }
    

    public void Create(string prefabId, string instanceId, string content, Vector3 position)
        => Create(prefabId, instanceId, content, position, isMoving: false);

    public void Create(string prefabId, string instanceId, LocalizedString content, Vector3 position)
        => Create(prefabId, instanceId, content, position, isMoving: false);

    public void Create(string prefabId, string instanceId, LocalizedString content)
        => Create(prefabId, instanceId, content.GetLocalizedString(), Vector3.zero, isMoving: false);
    
    
    public void Show(string id)
    {
        if (_activeTexts.TryGetValue(id, out GameObject textGo))
        {
            textGo.SetActive(true);
        }
        else
        {
            Debug.LogError($"[FloatingTextService] Floating text with instance id {id} not found");
        }
    }

    public void Hide(string id)
    {
        if (_activeTexts.TryGetValue(id, out GameObject textGo))
        {
            textGo.SetActive(false);
        }
        else
        {
            Debug.LogError($"[FloatingTextService] Floating text with instance id {id} not found");
        }
    }

    public void Destroy(string id, float time)
    {
        if (!_activeTexts.TryGetValue(id, out GameObject textGo))
        {
            Debug.LogError($"[FloatingTextService] Floating text with instance id {id} not found");
            return;
        }

        if (time <= 0f)
        {
            DestroyImmediately(id, textGo);
        }
        else
        {
            textGo.GetComponent<FloatingText>().StartCoroutine(DestroyWithDelayed(id, textGo, time));
        }
    }

    private IEnumerator DestroyWithDelayed(string id, GameObject instance, float time)
    {
        yield return new WaitForSeconds(time);
        if (_activeTexts.ContainsKey(id))
        {
            DestroyImmediately(id, instance);
        }
    }

    private void DestroyImmediately(string id, GameObject instance)
    {
        if(instance != null) 
            GameObject.Destroy(instance);
        
        _activeTexts.Remove(id);
    }
}