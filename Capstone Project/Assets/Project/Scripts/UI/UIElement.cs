using System;
using UnityEngine;

public abstract class UIElement : MonoBehaviour
{
    public string InstanceID { get; set; }
    public event Action<UIElement> OnClosed;

    public virtual void Initialize(string id)
    {
        InstanceID = id;
    }

    protected void Close()
    {
        OnClosed?.Invoke(this);
    }
}