using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization;
using UnityEngine;
using Object = System.Object;

public class PopupService : IPopupService
{
    private readonly Dictionary<PopupType, Popup> _popupsPrefab = new();
    private readonly Dictionary<Guid, Popup> _activePopups = new();
    
    private readonly GameObject _canvas;

    public PopupService(PopupContainer container, GameObject canvas)
    {
        _canvas = canvas;

        foreach (var popup in container.Popups.Where(popup => !_popupsPrefab.ContainsKey(popup.type)))
        {
            _popupsPrefab.Add(popup.type, popup.prefab);
        }
    }
    
    public void Create(PopupType popupType)
    {
        if (!_popupsPrefab.TryGetValue(popupType, out var popup))
        {
            Debug.LogError($"Popup Type {popupType} not found");
            return;
        }
        
        Popup popupInstance = UnityEngine.Object.Instantiate(popup, _canvas.transform);
        Guid id = Guid.NewGuid();
        _activePopups.Add(id, popupInstance);
    }

    public void Show(Guid id)
    {
        if (_activePopups.TryGetValue(id, out var popupInstance))
        {
            popupInstance.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError($"[PopupService]: Popup with {id} not found");
        }
    }

    public void Hide(Guid id)
    {
        if (_activePopups.TryGetValue(id, out var popupInstance))
        {
            popupInstance.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError($"[PopupService]: Popup with {id} not found");
        }
    }

    public void Destroy(Guid id, float time)
    {
        
    }
}