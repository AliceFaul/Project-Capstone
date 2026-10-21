using System.Collections.Generic;
using UnityEngine;

public class MinimapMarker : MonoBehaviour
{
    private static readonly List<MinimapMarker> all = new();

    public static IReadOnlyList<MinimapMarker> All => all;

    [SerializeField] private MinimapMarkerType type;
    [Tooltip("Optional. Position source of the icon. Defaults to this trasnform.")]
    [SerializeField] private Transform anchor;
    
    public MinimapMarkerType Type => type;
    public Vector3 WorldPosition => (anchor != null ? anchor : transform).position;
    
    /// <summary> Swap the icon at runtime (e.g. quest target -> completed). </summary>
    public void SetType(MinimapMarkerType newType) => type = newType;

    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => all.Clear();
}