using System;
using UnityEngine;

public class MinimapTarget : MonoBehaviour
{
    /// <summary> The active target, or null </summary>
    public static MinimapTarget Current { get; private set; }

    public static event Action<MinimapTarget> Changed;

    [Tooltip("Optional. Child whose forward is the icon arrow. Falls back to this transform.")]
    [SerializeField] private Transform facing;

    public Transform Facing => facing != null ? facing : transform;

    private void OnEnable()
    {
        Current = this;
        Changed?.Invoke(this);
    }

    private void OnDisable()
    {
        if(Current != null) return;
        Current = null;
        Changed?.Invoke(null);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = null;
        Changed = null;
    }
}