using System;
using UnityEngine;

/// <summary>
/// Root of ther minimap prefab, facade class and the ONLY thing other code talks to.
/// All parts find each other through this component, so nothing has to be dragged in the Inspector,
/// and the same prefab can be dropped in every scene (it bakes that scene's NavMesh on Start).
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance { get; private set; }

    private static readonly int AreaWindowId = Shader.PropertyToID("_MinimapAreaWindow");
    
    [Header("Settings")]
    [SerializeField] private string fallbackTargetTag = "Player";
    [SerializeField] private bool startVisible = false;

    [Header("Area window")]
    [Tooltip("Extra meters around the active area's radius that still count as part of its map.")]
    [SerializeField, Min(0f)] private float areaPadding = 2f;

    private MinimapMaskBaker _baker;
    public MinimapMaskBaker Baker
    {
        get
        {
            if(_baker == null) _baker = GetComponentInChildren<MinimapMaskBaker>(true);
            return _baker;
        }
        set => _baker = value;
    }
    
    private MinimapExploredMask _explored;
    public MinimapExploredMask Explored
    {
        get
        {
            if(_explored == null) _explored = GetComponentInChildren<MinimapExploredMask>(true);
            return _explored;
        }
        set => _explored = value;
    }
    
    private MinimapOverlayRenderer _overlay;
    public MinimapOverlayRenderer Overlay
    {
        get
        {
            if(_overlay == null) _overlay = GetComponentInChildren<MinimapOverlayRenderer>(true);
            return _overlay;
        }
        set => _overlay = value;
    }
    
    private MinimapIconLayer _icons;
    public MinimapIconLayer Icons
    {
        get
        {
            if(_icons == null) _icons = GetComponentInChildren<MinimapIconLayer>(true);
            return _icons;
        }
        set => _icons = value;
    }
    
    public Transform Target { get; private set; }
    public Transform Facing { get; private set; }
    
    public event Action TargetChanged;

    public bool HasAreaWindow { get; private set; }
    public Vector2 AreaWindowCenter { get; private set; }   // world X, Z
    public float AreaWindowRadius { get; private set; }     // meters, padding included

    /// <summary> Limit the minimap to a circle (the active area of the run). Starts a fresh fog of war inside it. </summary>
    public void SetAreaWindow(Vector3 center, float radius)
    {
        HasAreaWindow = true;
        AreaWindowCenter = new Vector2(center.x, center.z);
        AreaWindowRadius = radius + areaPadding;
        Shader.SetGlobalVector(AreaWindowId, new Vector4(AreaWindowCenter.x, AreaWindowCenter.y, AreaWindowRadius, 1f));

        if(Explored != null) Explored.OnAreaWindowChanged();
    }

    /// <summary> Show the whole level again (lobby, scenes without a run). </summary>
    public void ClearAreaWindow()
    {
        if(!HasAreaWindow) return;
        HasAreaWindow = false;
        Shader.SetGlobalVector(AreaWindowId, Vector4.zero);
    }

    public bool IsVisible => Overlay != null && Overlay.IsVisible;

    public void SetVisible(bool visible)
    {
        if(Overlay != null) Overlay.SetVisible(visible);
    }
    
    public void Toggle() => SetVisible(!IsVisible);

    /// <summary> Bake again, e.g. after building a NavMesh at runtime. </summary>
    public void Rebake()
    {
        if(Baker != null) Baker.Bake();
    }

    public void SetTarget(Transform target, Transform facing = null)
    {
        Target = target;
        Facing = facing != null ? facing : target;
        TargetChanged?.Invoke();
    }

    private void Start()
    {
        if(Target == null) FindTargetByTag();
        SetVisible(startVisible);

        // runs (node map levels) tell the minimap which area is active
        if(GetComponent<MinimapAreaLink>() == null) gameObject.AddComponent<MinimapAreaLink>();
    }

    private void OnEnable()
    {
        Instance = this;

        MinimapTarget.Changed += OnMinimapTargetChanged;
        if(MinimapTarget.Current != null) OnMinimapTargetChanged(MinimapTarget.Current);
    }

    private void OnDisable()
    {
        MinimapTarget.Changed -= OnMinimapTargetChanged;
        if(Instance == this) Instance = null;
        ClearAreaWindow(); // the shader global would otherwise stay set in the next scene
    }

    private void OnMinimapTargetChanged(MinimapTarget minimapTarget)
    {
        if(minimapTarget != null) SetTarget(minimapTarget.transform, minimapTarget.Facing);
        else SetTarget(null);
    }

    private void FindTargetByTag()
    {
        if(string.IsNullOrEmpty(fallbackTargetTag)) return;

        try
        {
            var go = GameObject.FindGameObjectWithTag(fallbackTargetTag);
            if (go != null) SetTarget(go.transform);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[MinimapSystem] Tag '{fallbackTargetTag}' is not defined. " +
                             "Add a MinimapTarget to the player instead.");
        }
    }
}