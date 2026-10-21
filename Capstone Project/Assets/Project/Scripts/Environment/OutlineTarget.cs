using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put on the ROOT of anything that can get a selection outline (enemy, NPC, interactable).
/// HoverSelector turns Hovered on/off; the renderer feature reads Renderers, Style and Intensity
/// through OutlineRegistry.
/// </summary>
public class OutlineTarget : MonoBehaviour
{
    [SerializeField] private OutlineStyle style;

    [Tooltip("Collect MeshRenderer/SkinnedMeshRenderer from this object and its children on Awake.")]
    [SerializeField] private bool autoCollectRenderers = true;
    
    [Tooltip("Used as-is when Auto Collect is off. Particle, trail and line renderers shouldn't be here.")]
    [SerializeField] private List<Renderer> renderers = new();
    
    [Header("Runtime (read-only)")]
    [SerializeField] private float intensity;
    [SerializeField] private bool hovered;
    [SerializeField] private bool locked;

    public OutlineStyle Style => style;
    public IReadOnlyList<Renderer> Renderers => renderers;
    
    /// <summary> 0..1, animated by fade in / out time of the style. </summary>
    public float Intensity => intensity;
    
    public bool IsHovered => hovered;
    public bool IsLocked => locked;

    private bool warnedNoStyle;

    private void Awake()
    {
        if(autoCollectRenderers) CollectRenderers();
    }

    private void OnDisable()
    {
        hovered = false;
        locked = false;
        intensity = 0f;
        OutlineRegistry.Unregister(this);
    }

    /// <summary> Mouse-over state, set by HoverSelector. </summary>
    public void SetHovered(bool value)
    {
        hovered = value;
        Refresh();
    }

    /// <summary> Stays outlined while true (for example the current attack target). </summary>
    public void SetLocked(bool value)
    {
        locked = value;
        Refresh();
    }

    private void Refresh()
    {
        if(!(hovered || locked)) return; // fade out is finished by the registry tick

        if (style == null)
        {
            if (warnedNoStyle) return;
            Debug.LogWarning($"[OutlineTarget] {name} has no OutlineStyle assigned.", this);
            warnedNoStyle = true;
            return;
        }
        
        OutlineRegistry.Register(this);
    }

    /// <summary> Call again if renderers change at runtime (equipment swap, model swap...). </summary>
    [ContextMenu("Auto Collect Renderers")]
    public void CollectRenderers()
    {
        renderers.Clear();
        
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if(r is MeshRenderer or SkinnedMeshRenderer) 
                renderers.Add(r);
        }
    }

    internal bool Tick(float dt)
    {
        if(style == null) return false;

        float goal = (hovered || locked) ? 1f : 0f;
        float time = goal > intensity ? style.fadeInTime : style.fadeOutTime;
        intensity = time <= 0f ? goal : Mathf.MoveTowards(intensity, goal, dt / time);
        
        return goal > 0f || intensity > 0f;
    }
}