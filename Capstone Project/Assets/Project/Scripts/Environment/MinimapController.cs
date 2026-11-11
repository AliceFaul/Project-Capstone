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
    
    [Header("Settings")]
    [SerializeField] private string fallbackTargetTag = "Player";
    [SerializeField] private bool startVisible = false;

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