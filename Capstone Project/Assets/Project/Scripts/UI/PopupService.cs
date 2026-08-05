using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine;

public class PopupService : IPopupService
{
    private Dictionary<string, GameObject> _popupsPrefab = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> _activePopups = new Dictionary<string, GameObject>();
    private GameObject _canvas;

    public PopupService()
    {
        
    }
    
    public void Create(string prefabID, string instanceID, LocalizedString content)
    {
        
    }

    public void Show(string id)
    {
        
    }

    public void Hide(string id)
    {
        
    }

    public void Destroy(string id, float time)
    {
        
    }

    public void Create(string prefabID, string instanceID, LocalizedString content, Action button1, Action button2)
    {
        
    }
}