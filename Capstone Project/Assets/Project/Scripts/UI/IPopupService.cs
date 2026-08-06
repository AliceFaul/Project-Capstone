using UnityEngine.Localization;
using System;

public enum PopupType
{
    Message,
    Warning,
    Confirm,
    Reward
}

public interface IPopupService : IUIService<PopupType>
{
    void Create(PopupType popupType);
}