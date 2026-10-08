using System;
using UnityEngine.Localization;

public interface IUIService
{
    void Create(string prefabId, string instanceId, LocalizedString content);
    void Show(string id);
    void Hide(string id);
    void Destroy(string id, float time);
}