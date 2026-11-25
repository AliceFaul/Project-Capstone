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
        [SerializeField] private Image placeholder;

        public void SetIcon(Sprite sprite)
        {
            if(icon == null) return;
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            if (placeholder != null) placeholder.enabled = sprite == null;
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

    [Header("Ranged Ammo")]
    [SerializeField] private TMP_Text ammoText;
    [Tooltip("Only the two refill tracks. Keep the arrow icon/count outside this group; hidden at full ammo.")]
    [SerializeField] private GameObject ammoRegenRoot;
    [Tooltip("Image Type = Filled, Method = Horizontal, Origin = Left.")]
    [SerializeField] private Image ammoRegenFill; // Left fill; keep name for existing prefab compatibility.
    [Tooltip("Image Type = Filled, Horizontal, Origin = Right.")]
    [SerializeField] private Image ammoRegenRightFill;
    [SerializeField] private Color ammoNormalColor = new Color(0.96f, 0.95f, 0.90f, 1f);
    [SerializeField] private Color ammoEmptyColor = new Color(1f, 0.35f, 0.30f, 1f);
    
    [Header("Lives")]
    [Tooltip("Parent of the icons: hidden when there is no run.")]
    [SerializeField] private GameObject livesRoot;
    [SerializeField] private Image[] lifeIcons = new Image[3];
    
    [Tooltip("Optinal. Empty = keep the sprite of each icons and only dim the lost ones.")]
    [SerializeField] private Sprite lifeSprite;
    [SerializeField] private Sprite lostSprite;
    [SerializeField] private Color lostColor = new Color(1f, 1f, 1f, 0.25f);

    private float _targetHealthFill = 1f;
    private float _targetExpValue = 0f;

    private int _shownLives = -1;
    private int _punchIndex = -1;
    private float _punchTime = 1f;
    
    private void Awake()
    {
        ClearCooldownOverlays(artifactSlots);
        ClearCooldownOverlays(abilitySlots);
        SetAmmoRegen(0f, false);
    }

    private void Update()
    {
        if(heartFill != null) heartFill.fillAmount = Mathf.Lerp(heartFill.fillAmount, _targetHealthFill, healthLerpSpeed * Time.deltaTime);
        if(expSlider != null) expSlider.value = Mathf.Lerp(expSlider.value, _targetExpValue, expLerpSpeed * Time.deltaTime);
        UpdateLifePunch();
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

    public void SetAmmo(int current, int maximum)
    {
        if(ammoText == null) return;
        maximum = Mathf.Max(0, maximum);
        current = Mathf.Clamp(current, 0, maximum);
        ammoText.text = current.ToString();
        ammoText.color = current > 0 ? ammoNormalColor : ammoEmptyColor;
    }

    public void SetAmmoRegen(float normalized, bool isRegenerating)
    {
        if(ammoRegenRoot != null && ammoRegenRoot.activeSelf != isRegenerating)
            ammoRegenRoot.SetActive(isRegenerating);

        float amount = isRegenerating ? Mathf.Clamp01(normalized) : 0f;
        if (ammoRegenFill != null)
        {
            ammoRegenFill.fillAmount = amount;
            ammoRegenFill.enabled = isRegenerating;
        }
        if (ammoRegenRightFill != null)
        {
            ammoRegenRightFill.fillAmount = amount;
            ammoRegenRightFill.enabled = isRegenerating;
        }
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

    public void SetLives(int lives, int max, bool animate)
    {
        if(livesRoot != null) livesRoot.SetActive(max > 0);

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            Image icon = lifeIcons[i];
            if (icon == null) continue;

            bool visible = i < max;
            icon.gameObject.SetActive(visible);
            if (!visible) continue;

            bool alive = i < lives;
            if (lifeSprite != null) icon.sprite = alive ? lifeSprite : (lostSprite != null ? lostSprite : lifeSprite);
            icon.color = alive || lostSprite != null ? Color.white : lostColor;
            icon.transform.localScale = Vector3.one;
        }
        
        if (animate && _shownLives > lives && lives >= 0 && lives < lifeIcons.Length)
        {
            _punchIndex = lives;
            _punchTime = 0f;
        }
        _shownLives = lives;
    }
    
    private void UpdateLifePunch()
    {
        if (_punchIndex < 0 || _punchIndex >= lifeIcons.Length || lifeIcons[_punchIndex] == null) return;

        _punchTime += Time.unscaledDeltaTime / 0.4f;
        float scale = _punchTime < 1f ? 1f + 0.5f * Mathf.Sin(_punchTime * Mathf.PI) : 1f;
        lifeIcons[_punchIndex].transform.localScale = Vector3.one * scale;
        if (_punchTime >= 1f) _punchIndex = -1;
    }
}
