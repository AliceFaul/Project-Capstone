using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization;
using UnityEngine;

public class PopupService : IPopupService
{
    private readonly Dictionary<string, GameObject> _popupsPrefab = new();
    private readonly Dictionary<string, GameObject> _activePopups = new();
    
    private GameObject _canvas;

    public PopupService()
    {
        PopupContainer popup = ResourceManager.Instance.GetAsset<PopupContainer>("PopupContainer");

        if (popup == null)
        {
            Debug.LogError($"[PopupService] PopupContainer not found! Need to preload in Resource Manager.");
            return;
        }
        
        foreach (var entry in popup.Popups)
        {
            _popupsPrefab[entry.id] = entry.prefab;
        }
    }
    
    public GameObject Create(string prefabId, string instanceId, LocalizedString content, Func<bool> onClick1, Func<bool> onClick2)
    {
        if (_activePopups.ContainsKey(instanceId)) Destroy(instanceId, 0f);

        if (_canvas == null)
        {
            CanvasCreator canvasCreator = new CanvasCreator();
            _canvas = canvasCreator.Create(false);
        }

        if (!_popupsPrefab.TryGetValue(prefabId, out var prefab) || prefab == null)
        {
            Debug.LogError($"[PopupService] Prefab with id '{prefabId}' not found!");
            return null;
        }

        GameObject popupGo = GameObject.Instantiate(prefab, _canvas.transform);

        if (popupGo.TryGetComponent(out Popup popup)) popup.Setup(instanceId, content, onClick1, onClick2);
        else Debug.LogError($"[PopupService] Prefab '{prefabId}' is missing the Popup component!");

        _activePopups[instanceId] = popupGo;
        return popupGo;
    }

    public GameObject Create(string prefabId, string instanceId, LocalizedString content, Func<bool> onClick1)
        => Create(prefabId, instanceId, content, onClick1, null);

    public GameObject Create(string prefabId, string instanceId, LocalizedString content)
        => Create(prefabId, instanceId, content, null, null);

    public void Show(string id)
    {
        if (_activePopups.TryGetValue(id, out var popupInstance))
        {
            popupInstance.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError($"[PopupService] Popup with {id} not found");
        }
    }

    public void Hide(string id)
    {
        if (_activePopups.TryGetValue(id, out var popupInstance))
        {
            popupInstance.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError($"[PopupService] Popup with {id} not found");
        }
    }

    public void Destroy(string id, float time)
    {
        if (_activePopups.TryGetValue(id, out var popupInstance))
        {
            if(popupInstance != null) GameObject.Destroy(popupInstance.gameObject, time);
            _activePopups.Remove(id);
        }
        else
        {
            Debug.LogError($"[PopupService] Popup with instanceId {id} not found");
        }
    }
}