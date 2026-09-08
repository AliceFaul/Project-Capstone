using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using GDX.Collections.Generic;
using System;
using System.Timers;

public class StatusEffectIcon
{
    public readonly GameObject StatusIconContainer;
    public readonly Image StatusBuildupFill;
    public readonly Color StatusBuildupColor;
    public readonly Image StatusIcon;
    
    public IStatusEffect BoundEffect;
    public Action<float, float> ProgressedHandler;
    public Action DeactivatedHandler;
    public float TargetFill;
    public Coroutine FillRoutine;

    public StatusEffectIcon(GameObject statusIconContainer, Image statusBuildupFill, Color statusBuildupColor,
        Image statusIcon)
    {
        StatusIconContainer = statusIconContainer;
        StatusBuildupFill = statusBuildupFill;
        StatusBuildupColor = statusBuildupColor;
        StatusIcon = statusIcon;
    }
}

public class StatusEffectUI : MonoBehaviour
{
    [Header("Status Effects UI Settings")]
    [Tooltip("Prefab status effect icon reference")]
    [SerializeField] private GameObject statusEffectIcon;
    [Tooltip("Dictionary of status effect icon values")]
    [SerializeField] private SerializableDictionary<StatusEffectType, Sprite> statusEffectSprites;
    [Tooltip("Dictionary of status effect color buildup fill image")]
    [SerializeField] private SerializableDictionary<StatusEffectType, Color> statusEffectColors;
    
    [Header("Fill Smoothing")]
    [SerializeField] private float smoothing = 1f;

    private readonly Dictionary<StatusEffectType, StatusEffectIcon> _statusEffectIcons = new();
    
    // Call this function when apply status effect in entity IAttackable like enemy, props, etc...
    public void ShowEffects(IStatusEffect effect)
    {
        if(effect == null)
            return;
        
        var icon = GetOrCreateIcon(effect.StatusType);
        
        // Delete old to create a new status effect in IAttackable entity
        if(icon.BoundEffect != null)
            UnbindIcon(icon);
        
        icon.BoundEffect = effect;
        icon.ProgressedHandler = (elapsed, duration) => OnUpdateStatusEffect(icon, elapsed, duration);
        icon.DeactivatedHandler = () => OnDeactivateStatusEffect(icon);
        
        // Run progress by lambda
        effect.Progressed += icon.ProgressedHandler;
        effect.Deactivated += icon.DeactivatedHandler;
        OnActivateStatus(icon);
    }
    
    private StatusEffectIcon GetOrCreateIcon(StatusEffectType type)
    {
        // Get icon if we have existing effect
        if (_statusEffectIcons.TryGetValue(type, out var existing))
        {
            existing.StatusIconContainer.SetActive(true);
            return existing;
        }
        
        // Create new status effect icon
        GameObject newIcon = Instantiate(statusEffectIcon, transform);
        var statusBuildupFill = newIcon.GetComponent<Image>();
        Color color = statusEffectColors.TryGetValue(type, out var mappedColor) 
            ? mappedColor : Color.white;
        
        statusBuildupFill.color = color;
        statusBuildupFill.fillAmount = 0f;
        
        var iconImage = newIcon.transform.Find("Icon").GetComponent<Image>();
        if (iconImage != null && statusEffectSprites.TryGetValue(type, out var sprite))
        {
            iconImage.sprite = sprite;
        }
        else
        {
            Debug.LogError($"[StatusEffectUI] Not found {type} icon sprite in dictionary or icon image can't found!");
        }
        
        newIcon.SetActive(true);
        var newStatusIcon = new StatusEffectIcon(newIcon, statusBuildupFill, color, iconImage);
        _statusEffectIcons[type] = newStatusIcon;
        return newStatusIcon;
    }
    
    private void OnActivateStatus(StatusEffectIcon icon)
    {
        icon.StatusIconContainer.SetActive(true);
        icon.StatusBuildupFill.fillAmount = 0f;
        icon.TargetFill = 0f;
        
        if(icon.FillRoutine != null)
            StopCoroutine(icon.FillRoutine);
        
        icon.FillRoutine = StartCoroutine(SmoothFill(icon));
    }

    private void OnUpdateStatusEffect(StatusEffectIcon icon, float elapsed, float duration)
    {
        if(icon.StatusIconContainer == null)
            return;

        icon.TargetFill = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;
    }

    private IEnumerator SmoothFill(StatusEffectIcon icon)
    {
        while (icon.BoundEffect != null)
        {
            icon.StatusBuildupFill.fillAmount = Mathf.MoveTowards(
                icon.StatusBuildupFill.fillAmount, 
                icon.TargetFill, 
                smoothing * Time.deltaTime);
            yield return null;
        }
    }

    private void OnDeactivateStatusEffect(StatusEffectIcon icon)
    {
        if(icon.StatusIconContainer == null)
            return;
        
        UnbindIcon(icon);
        icon.StatusIconContainer.SetActive(false);
    }

    private void UnbindIcon(StatusEffectIcon icon)
    {
        if (icon.BoundEffect != null)
        {
            if(icon.ProgressedHandler != null)
                icon.BoundEffect.Progressed -= icon.ProgressedHandler;
            if(icon.DeactivatedHandler != null)
                icon.BoundEffect.Deactivated -= icon.DeactivatedHandler;
        }
        
        icon.BoundEffect = null;
        icon.ProgressedHandler = null;
        icon.DeactivatedHandler = null;

        if (icon.FillRoutine != null)
        {
            StopCoroutine(icon.FillRoutine);
            icon.FillRoutine = null;
        }
    }
}