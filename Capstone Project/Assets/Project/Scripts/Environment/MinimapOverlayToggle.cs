using System;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Show / hides the minimap ground overlay (Tab by default, via the ToggleMinimap input action).
/// Only the overlay cube's MeshRenderer is toggled. That means:
///     - nothing is drawn while hidden (no GPU cost).
///     - MinimapOverlayProjector keeps following the player.
///     - MinimapExploredMask keeps revealing the map while hidden.
/// </summary>
public class MinimapOverlayToggle : MonoBehaviour
{
    private const string PrefKey = "Minimap.OverlayVisible";
    
    [Header("References")]
    [Tooltip("Leave empty to auto-find the PlayerController to get InputHandler in the scene.")]
    [SerializeField] private PlayerController controller;
    [Tooltip("MeshRenderer of the overlay cube (the object with MinimapOverlayProjector).")]
    [SerializeField] private MeshRenderer overlayRenderer;
    
    [Header("Settings")]
    [SerializeField] private bool startVisible = true;
    [Tooltip("Remember the last on/off choice between sessions (PlayerPrefs).")]
    [SerializeField] private bool rememberChoice = true;
    
    [Header("Events")]
    [Tooltip("Hook UI here, e.g. highlight the map icon. Passes the new visibility.")]
    public UnityEvent<bool> onVisibilityChanged;

    private InputHandler _inputHandler;
    private bool subcribed;
    
    public bool IsVisible { get; private set; }
    /// <summary> Code-side version of onVisibilityChanged. </summary>
    public event Action<bool> VisibilityChanged;

    private void Awake()
    {
        if(controller == null) controller = FindFirstObjectByType<PlayerController>();
        _inputHandler = controller.InputHandler;
    }

    private void Start()
    {
        // the player may be spawned after this object
        if (!subcribed)
        {
            if(controller == null) controller = FindFirstObjectByType<PlayerController>();
            _inputHandler = controller.InputHandler;
            
            Subscribe();
            
            if(!subcribed) Debug.LogWarning($"[MinimapOverlayToggle] No InputHandler found, toggle key wil not work.");
        }
        
        bool visible = startVisible;
        if(rememberChoice && PlayerPrefs.HasKey(PrefKey)) visible = PlayerPrefs.GetInt(PrefKey) == 1;
        Apply(visible, false);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if(subcribed && _inputHandler != null) _inputHandler.OnToggleMinimap -= Toggle;
        subcribed = false;
    }

    private void Subscribe()
    {
        if(subcribed || _inputHandler == null) return;
        _inputHandler.OnToggleMinimap += Toggle;
        subcribed = true;
    }

    public void Toggle() => SetVisible(!IsVisible);
    public void SetVisible(bool visible) => Apply(visible, true);

    private void Apply(bool visible, bool save)
    {
        IsVisible = visible;
        
        if(overlayRenderer != null) overlayRenderer.enabled = visible;
        if(save && rememberChoice) PlayerPrefs.SetInt(PrefKey, visible ? 1 : 0);
        
        VisibilityChanged?.Invoke(visible);
        onVisibilityChanged?.Invoke(visible);
    }
}