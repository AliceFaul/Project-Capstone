using UnityEngine.Localization;
using System;

public interface IPopupService : IUIService
{
    void Create(string prefabID, string instanceID, LocalizedString content, Action button1, Action button2);
}