using System;
using System.Collections.Generic;
using System.Linq;
using TMPro.EditorUtilities;
using UnityEngine.Localization;
using UnityEngine;
using Object = System.Object;

public class PopupService : IPopupService
{
    private Dictionary<string, GameObject> _popupsPrefab = new();
    private Dictionary<string, GameObject> _activePopups = new();
    
    private GameObject _canvas;

    public PopupService()
    {
        PopupContainer popup = ResourceManager.Instance.GetAsset<PopupContainer>("PopupContainer");
        foreach (var entry in popup.Popups)
        {
            _popupsPrefab[entry.id] = entry.prefab;
        }
    }
    
    public void Create(string prefabId, string instanceId, LocalizedString content, Action onClick1, Action onClick2)
    {
        if (_activePopups.ContainsKey(instanceId))
        {
            Destroy(instanceId, 0f);
        }

        if (_canvas == null)
        {
            CanvasCreator canvasCreator = new CanvasCreator();
            _canvas = canvasCreator.Create(false);
        }

        GameObject popupGo = GameObject.Instantiate(_popupsPrefab[prefabId], _canvas.transform);
        Popup popup = popupGo.GetComponent<Popup>();

        if (onClick1 == null && onClick2 == null)
        {
            popup.Setup(instanceId, content);
        }
        else
        {
            popup.Setup(instanceId, content, onClick1, onClick2);
        }
        
        _activePopups.Add(instanceId, popupGo);
    }
    
    public void Create(string prefabId, string instanceId, LocalizedString content)
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
            GameObject.Destroy(popupInstance.gameObject);
            _activePopups.Remove(id);
        }
        else
        {
            Debug.LogError($"[PopupService] Popup with instanceId {id} not found");
        }
    }
}