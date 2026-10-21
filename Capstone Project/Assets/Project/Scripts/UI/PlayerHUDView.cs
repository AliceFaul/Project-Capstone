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
    [Tooltip("Tốc độ chạy mượt của thanh máu.")]
    [SerializeField] private float healthLerpSpeed = 10f;
    
    [Header("Level / Experience")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider expSlider;
    [Tooltip("Tốc độ chạy mượt của thanh kinh nghiệm.")]
    [SerializeField] private float expLerpSpeed = 8f;
    
    [Header("Artifact Slots")]
    [SerializeField] private SlotView[] artifactSlots = new SlotView[3];
    
    [Header("Ability Slots")]
    [SerializeField] private SlotView[] abilitySlots = new SlotView[2];

    private float _targetHealthFill = 1f;
    private float _targetExpValue = 0f;
    
    private void Awake()
    {
        ClearCooldownOverlays(artifactSlots);
        ClearCooldownOverlays(abilitySlots);
    }

    private void Update()
    {
        if(heartFill != null) heartFill.fillAmount = Mathf.Lerp(heartFill.fillAmount, _targetHealthFill, healthLerpSpeed * Time.deltaTime);
        if(expSlider != null) expSlider.value = Mathf.Lerp(expSlider.value, _targetExpValue, expLerpSpeed * Time.deltaTime);
    }

    public void SetHealth(float current, float maximum)
    {
        if(heartFill == null) return;
        _targetHealthFill = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
    }

    public void SetLevel(int level)
    {
        if(levelText == null) return;
        levelText.text = level.ToString();
    }

    public void SetExperience(float current, float required)
    {
        if(expSlider == null) return;
        _targetExpValue = required > 0f ? Mathf.Clamp01(current / required) : 0f;
    }

    public void SnapHealth(float current, float maximum)
    {
        if(heartFill == null) return;
        _targetHealthFill = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        heartFill.fillAmount = _targetHealthFill;
    }

    public void SnapExperience(float current, float required)
    {
        if(expSlider == null) return;
        _targetExpValue = required > 0f ? Mathf.Clamp01(current / required) : 0f;
        expSlider.value = _targetExpValue;
    }

    public void SetArtifactIcon(int slot, Sprite icon) => SetIcon(artifactSlots, slot, icon);
    public void SetAbilityIcon(int slot, Sprite icon) => SetIcon(abilitySlots, slot, icon);
    public void SetArtifactCooldown(int slot, float normalized) => SetCooldown(artifactSlots, slot, normalized);
    public void SetAbilityCooldown(int slot, float normalized) => SetCooldown(abilitySlots, slot, normalized);

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
