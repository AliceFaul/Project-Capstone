using UnityEngine.Localization;
using System;
using UnityEditor;

public interface IPopupService : IUIService
{
    void Create(string prefabId, 
                string instanceId, 
                LocalizedString content, 
                Action onClick1, 
                Action onClick2);
    
    void Create(string prefabId, 
                string instanceId, 
                LocalizedString content, 
                Action onClick1);
}