using System;
using UnityEngine;

/// <summary>
/// Finds the OutlineTarget under the mouse and flips its Hovered state.
/// The raycast works like the click raycasts in PlayerController.
/// </summary>
public class HoverSelector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty will use Camera.main.")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("Leave empty to auto-find the PlayerController in the scene.")]
    [SerializeField] private PlayerController controller;
    
    [Header("Settings")]
    [SerializeField] private LayerMask hoverLayers;
    [SerializeField, Min(1f)] private float maxDistance = 200f;
    [Tooltip("Print hover changes to the Console (for testing).")]
    [SerializeField] private bool logChanges;

    private InputHandler _inputHandler;
    
    /// <summary> Set to false will disable hover (dead, cutscene, menu open...). Clears the current hover. </summary>
    public bool HoverEnabled { get; set; } = true;
    public OutlineTarget Hovered { get; private set; }

    /// <summary> (previous, current). Either can be null. </summary>
    public event Action<OutlineTarget, OutlineTarget> HoveredChanged;

    private void Awake()
    {
        if(mainCamera == null) mainCamera = Camera.main;
        if(controller == null) controller = FindFirstObjectByType<PlayerController>();
        _inputHandler = controller != null ? controller.InputHandler : FindFirstObjectByType<InputHandler>();
    }

    private void OnDisable()
    {
        SetHovered(null);
    }

    private void Update()
    {
        SetHovered(HoverEnabled ? Pick() : null);
    }

    private OutlineTarget Pick()
    {
        if(mainCamera == null || _inputHandler == null) return null;
        
        Vector2 mouse = _inputHandler.MousePosition;
        if(UIInputBlocker.IsPointerOverUI(mouse)) return null;
        
        Ray ray = mainCamera.ScreenPointToRay(mouse);
        if(!Physics.Raycast(ray, out var hit, maxDistance, ~0, QueryTriggerInteraction.Ignore)) return null;
        
        int layerBit = 1 << hit.collider.gameObject.layer;
        if((hoverLayers.value & layerBit) == 0) return null;
        return hit.collider.GetComponentInParent<OutlineTarget>();
    }

    private void SetHovered(OutlineTarget next)
    {
        if(next == Hovered) return;

        OutlineTarget previous = Hovered;
        if(previous != null) previous.SetHovered(false);
        
        Hovered = next;
        if(next != null) next.SetHovered(true);
        
        if(logChanges) Debug.Log($"[HoverSelector] {(previous != null ? previous.name : "none")} -> {(next != null ? next.name : "none")}");
        HoveredChanged?.Invoke(previous, next);
    }
}