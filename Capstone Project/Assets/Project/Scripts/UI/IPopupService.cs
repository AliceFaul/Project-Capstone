using UnityEngine.Localization;
using System;
using UnityEngine;

public interface IPopupService
{
    GameObject Create(string prefabId, 
                string instanceId, 
                LocalizedString content, 
                Func<bool> onClick1, 
                Func<bool> onClick2);
    
    GameObject Create(string prefabId, 
                string instanceId, 
                LocalizedString content, 
                Func<bool> onClick1);

    GameObject Create(string prefabId, 
                string instanceId, 
                LocalizedString content);

    void Show(string id);
    void Hide(string id);
    void Destroy(string id, float time = 0f);
}