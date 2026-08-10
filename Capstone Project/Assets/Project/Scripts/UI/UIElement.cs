using System;
using UnityEngine;

public abstract class UIElement : MonoBehaviour
{
    public Guid InstanceID { get; set; }
    public event Action<UIElement> OnClosed;

    public virtual void Initialize(Guid id)
    {
        InstanceID = id;
    }

    protected void Close()
    {
        OnClosed?.Invoke(this);
    }
}