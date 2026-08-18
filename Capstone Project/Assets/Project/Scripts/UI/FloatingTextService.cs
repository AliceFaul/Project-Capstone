using UnityEngine;
using UnityEngine.Localization;
using System.Collections.Generic;

public class FloatingTextService : IFloatingTextService
{
    private Dictionary<string, GameObject> _textPrefabs = new();
    private Dictionary<string, GameObject> _activeTexts = new();

    private GameObject _canvas;

    public FloatingTextService()
    {
        FloatingTextContainer textContainer = ResourceManager.Instance.GetAsset<FloatingTextContainer>("FloatingTextContainer");
        foreach (var entry in textContainer.FloatingTexts)
        {
            _textPrefabs[entry.id] = entry.prefab;
        }
    }

    public void Create(string prefabId, string instanceId, string content, Vector3 position)
    {
        if (_canvas == null)
        {
            CanvasCreator canvasCreator = new CanvasCreator();
            _canvas = canvasCreator.Create(true);
        }
        
        GameObject textGo = GameObject.Instantiate(_textPrefabs[prefabId], _canvas.transform);
        textGo.transform.position = position;

        if (textGo.GetComponent<FloatingText>())
        {
            textGo.GetComponent<FloatingText>().SetText(content);
        }
        
        _activeTexts.Add(instanceId, textGo);
    }
    
    public void Create(string prefabId, string instanceId, LocalizedString content, Vector3 position, bool isMoving)
    {
        if (_canvas == null)
        {
            CanvasCreator canvasCreator = new CanvasCreator();
            _canvas = canvasCreator.Create(true);
        }
        
        GameObject textGo = GameObject.Instantiate(_textPrefabs[prefabId], _canvas.transform);
        textGo.transform.position = position;

        if (textGo.GetComponent<FloatingText>())
        {
            textGo.GetComponent<FloatingText>().SetText(content.GetLocalizedString());
        }

        if (isMoving)
        {
            textGo.AddComponent<ObjectMoving>();
        }
        
        _activeTexts.Add(instanceId, textGo);
    }

    public void Create(string prefabId, string instanceId, LocalizedString content, Vector3 position)
        => Create(prefabId, instanceId, content.GetLocalizedString(), position);

    public void Create(string prefabId, string instanceId, LocalizedString content)
        => Create(prefabId, instanceId, content.GetLocalizedString(), Vector3.zero);
    
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
        if (_activeTexts.TryGetValue(id, out GameObject textGo))
        {
            GameObject.Destroy(textGo, time);
            _activeTexts.Remove(id);
        }
        else
        {
            Debug.LogError($"[FloatingTextService] Floating text with instance id {id} not found]");
        }
    }
}