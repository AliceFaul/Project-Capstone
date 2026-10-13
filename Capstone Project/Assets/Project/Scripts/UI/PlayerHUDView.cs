using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDView : MonoBehaviour
{
    [Serializable]
    private class SlotView
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image cooldownOverlay;

        public void SetIcon(Sprite sprite)
        {
            if(icon == null) return;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        public void SetCooldown(float normalized)
        {
            if(cooldownOverlay == null) return;
            normalized = Mathf.Clamp01(normalized);
            cooldownOverlay.fillAmount = normalized;
            cooldownOverlay.enabled = normalized > 0.001f;
        }
    }
    
    [Header("Health")]
    [SerializeField] private Image heartFill;
    
    [Header("Level / Experience")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider expSlider;
    
    [Header("Artifact Slots")]
    [SerializeField] private SlotView[] artifactSlots = new SlotView[3];
    
    [Header("Ability Slots")]
    [SerializeField] private SlotView[] abilitySlots = new SlotView[2];

    private void Awake()
    {
        ClearCooldownOverlays(artifactSlots);
        ClearCooldownOverlays(abilitySlots);
    }

    public void SetHealth(float current, float maximum)
    {
        if(heartFill == null) return;
        heartFill.fillAmount = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
    }

    public void SetLevel(int level)
    {
        if(levelText == null) return;
        levelText.text = level.ToString();
    }

    public void SetExperience(float current, float required)
    {
        if(expSlider == null) return;
        expSlider.value = required > 0f ? Mathf.Clamp01(current / required) : 0f;
    }

    private static void SetIcon(SlotView[] slots, int index, Sprite icon)
    {
        if(slots == null || index < 0 || index >= slots.Length || slots[index] == null) return;
        slots[index].SetIcon(icon);
    }

    private static void SetCooldown(SlotView[] slots, int index, float normalized)
    {
        if(slots == null || index < 0 || index >= slots.Length || slots[index] == null) return;
        slots[index].SetCooldown(normalized);
    }

    private static void ClearCooldownOverlays(SlotView[] slots)
    {
        if(slots == null) return;
        foreach (var slot in slots) slot?.SetCooldown(0f);
    }
}
