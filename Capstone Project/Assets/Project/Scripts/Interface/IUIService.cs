using System;
using UnityEngine.Localization;

public interface IUIService<in TType>
{
    void Create(TType type);
    void Show(Guid id);
    void Hide(Guid id);
    void Destroy(Guid id, float time);
}