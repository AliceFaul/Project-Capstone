using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class StatusEffectIcon
{
    public readonly GameObject StatusIconContainer;
    public Image StatusBuildupFill;
    public Color StatusBuildupColor;
    public Image StatusIcon;

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
    [SerializeField] private GameObject statusEffectIcon;
    [SerializeField] private SerializedDictionary<StatusEffectType, Sprite> statusEffectSprites;
    [SerializeField] private SerializedDictionary<StatusEffectType, Color> statusEffectColors;
    
    private readonly Dictionary<StatusEffectData, StatusEffectIcon> _statusEffectIcons = new();

    private StatusEffectIcon CreateStatusEffectIcon(StatusEffectData statusEffectData)
    {
        if (_statusEffectIcons.ContainsKey(statusEffectData))
        {
            _statusEffectIcons[statusEffectData].StatusIconContainer.SetActive(true);
            return _statusEffectIcons[statusEffectData];
        }
        
        GameObject newStatusIcon = Instantiate(statusEffectIcon, transform);
        var statusBuildupFill = newStatusIcon.GetComponent<Image>();
        Color color = statusEffectColors[statusEffectData.type];
        
        statusBuildupFill.color = color;
        statusBuildupFill.fillAmount = 0;
        
        var icon = newStatusIcon.transform.Find("icon").GetComponent<Image>();
        icon.sprite = statusEffectSprites[statusEffectData.type];
        newStatusIcon.SetActive(true);
        return new StatusEffectIcon(newStatusIcon, statusBuildupFill, color, icon);
    }
    
    private void OnActivateStatus(float buildAmount)
    {
        
    }

    private void OnUpdateStatusEffect(float buildAmount, float duration)
    {
        
    }

    private void OnDeactivateStatusEffect(float buildAmount)
    {
        
    }

    private void UpdateBuildupAndDuration(float buildupAmount, float duration)
    {
        
    }
}