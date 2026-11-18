using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Project.Capstone.Inventory;

// Modal popup for the sockets of ONE equipped weapon: shows its 3 sockets, lets the player pick a rune from the rune bag,
// preview it (quality, description, same-group warning) and insert it, or remove a socketed rune (two clicks, costs Gem).
// Esc is handled by InventoryScreenView (it calls Close()).
public class RuneSocketPopup : MonoBehaviour
{
    [Header("Backdrop")]
    [SerializeField] private Image backdrop; // full-screen image behind the panel (blur material goes here)

    [Header("Weapon")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TMP_Text weaponLevel;
    [SerializeField] private TMP_Text weaponName;
    [SerializeField] private TMP_Text weaponSub;

    [Header("Sockets")]
    [SerializeField] private RuneSocketRowView[] rows;

    [Header("Rune bag")]
    [SerializeField] private Transform bagContent;
    [SerializeField] private InventorySlotView bagSlotPrefab;
    [SerializeField] private TMP_Text bagCountText;
    [SerializeField] private RectTransform selectionFrame;

    [Header("Rune card")]
    [SerializeField] private GameObject cardContent;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text runeName;
    [SerializeField] private Image rarityBg;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private Transform rowsRoot;
    [SerializeField] private GameObject statRowPrefab;
    [SerializeField] private GameObject descGroup;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private GameObject warnGroup;
    [SerializeField] private TMP_Text warnText;

    [Header("Buttons")]
    [SerializeField] private Button insertButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text gemText;

    [Serializable]
    private struct StatIconEntry { public BonusStat stat; public Sprite icon; }
    [Header("Visuals")]
    [SerializeField] private StatIconEntry[] statIcons;
    [SerializeField] private Sprite durationIcon, effectIcon, meleeFallbackIcon, rangedFallbackIcon;
    [SerializeField] private GameObject qualityGroup;
    [SerializeField] private RectTransform qualityFill;
    [SerializeField] private TMP_Text qualityValue;
    [SerializeField] private ScrollRect bagScroll;

    public event Action<string> Message;
    public event Action Closed;

    public bool IsOpen => gameObject.activeSelf;
    public Image Backdrop => backdrop;

    private PlayerDataConfig _data;
    private EquipmentManager _equip;
    private EquipSlot _weaponSlot;
    private int _socketIndex;
    private RuneInstance _selected;
    private int _armedIndex = -1;
    private float _armedUntil;
    private bool _subscribed;
    private readonly List<InventorySlotView> _views = new List<InventorySlotView>();

    private void Awake()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].index = i;
            rows[i].Clicked += OnRowClicked;
            rows[i].RemoveClicked += OnRemoveClicked;
        }
        insertButton.onClick.AddListener(OnInsertClicked);
        cancelButton.onClick.AddListener(Close);
    }

    public void Open(PlayerDataConfig data, EquipmentManager equip, EquipSlot weaponSlot, int socketIndex, RuneInstance preselect)
    {
        bool wasOpen = IsOpen;
        if (wasOpen) Unsubscribe();
        _data = data;
        _equip = equip;
        _weaponSlot = weaponSlot;
        _socketIndex = Mathf.Clamp(socketIndex, 0, EquipmentInstance.SocketCount - 1);
        _selected = preselect;
        _armedIndex = -1;

        gameObject.SetActive(true); // OnEnable refreshes
        if (wasOpen) { Subscribe(); Refresh(); }
    }

    public void Close() => gameObject.SetActive(false);

    private void OnEnable()
    {
        if (_data == null || _equip == null) return;
        Subscribe();
        Refresh();
        if (isActiveAndEnabled) StartCoroutine(FocusFirst());
    }

    private void Subscribe()
    {
        if (_subscribed || _data == null || _equip == null) return;
        _equip.OnEquipmentStatsChanged += OnStatsChanged;
        _equip.OnLoadoutReloaded += Refresh;
        _data.Inventory.OnBagChanged += OnBagChanged;
        _data.Currency.OnCurrencyChanged += OnCurrencyChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        _subscribed = false;
        if (_equip != null)
        {
            _equip.OnEquipmentStatsChanged -= OnStatsChanged;
            _equip.OnLoadoutReloaded -= Refresh;
        }
        if (_data != null)
        {
            _data.Inventory.OnBagChanged -= OnBagChanged;
            _data.Currency.OnCurrencyChanged -= OnCurrencyChanged;
        }
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        Unsubscribe();
        Closed?.Invoke();
    }

    private void OnLocaleChanged(Locale locale) => Refresh();

    private void Update()
    {
        if (_armedIndex >= 0 && Time.unscaledTime > _armedUntil) Disarm();
    }

    private IEnumerator FocusFirst()
    {
        yield return null;
        var eventSystem = EventSystem.current;
        if (eventSystem == null) yield break;

        foreach (var view in _views)
        {
            if (!view.gameObject.activeSelf || view.Item == null) continue;
            if (_selected != null && view.Item.InstanceId != _selected.instanceId) continue;
            eventSystem.SetSelectedGameObject(view.gameObject);
            yield break;
        }
        eventSystem.SetSelectedGameObject(cancelButton.gameObject);
    }

    private void OnStatsChanged(EquipSlot slot) => Refresh();
    private void OnBagChanged(InventoryCategory category) { if (category == InventoryCategory.Rune) Refresh(); }
    private void OnCurrencyChanged(CurrencyType type, int amount) => gemText.text = _data.Currency.Gem().ToString("N0");

    // ---------------------------------------------------------------- refresh

    private void Refresh()
    {
        if (_data == null || _equip == null) return;

        var weapon = _equip.GetEquipped(_weaponSlot);
        if (weapon == null)
        {
            Close();
            return;
        }

        _armedIndex = -1;
        RefreshWeapon(weapon);
        RefreshRows(weapon);
        RebuildRuneGrid();
        RefreshCard(weapon);
        RefreshButtons(weapon);
    }

    private void RefreshWeapon(EquipmentInstance weapon)
    {
        var def = Def(weapon);
        var rarity = def != null ? def.rarity : Rarity.Common;
        bool hasSprite = def != null && def.icon != null;

        weaponIcon.sprite = hasSprite ? def.icon : weapon.category == InventoryCategory.Melee ? meleeFallbackIcon : rangedFallbackIcon;
        weaponIcon.enabled = weaponIcon.sprite != null;
        weaponIcon.color = Color.white;
        weaponLevel.text = weapon.level.ToString();
        weaponName.text = def != null ? (def.itemName ?? "").ToUpper() : InventoryText.Get("DETAIL_UNKNOWN_ITEM");

        string kind = InventoryText.Get(weapon.category == InventoryCategory.Melee ? "DETAIL_KIND_MELEE" : "DETAIL_KIND_RANGED");
        var equipment = def as EquipmentData;
        weaponSub.text = equipment != null
            ? $"{kind} - {InventoryText.Get("WEAPON_" + equipment.weaponType.ToString().ToUpper())}"
            : kind;
    }

    private void RefreshRows(EquipmentInstance weapon)
    {
        var sockets = _equip.Sockets;
        var cfg = _equip.ProgressConfig;
        if (sockets == null || cfg == null) return;

        int cost = sockets.GetRemovalCost(weapon);
        var activeRunes = RuneResolver.GetActiveRunes(weapon, cfg.GetRune);

        for (int i = 0; i < rows.Length; i++)
        {
            bool unlocked = sockets.IsSocketUnlocked(weapon, i);
            var rune = weapon.GetSocket(i);

            if (!unlocked)
            {
                int need = UnlockLevel(i);
                rows[i].Bind(SocketRowState.Locked, InventoryText.Get("POPUP_SOCKET_LOCKED", i + 1),
                    InventoryUIUtil.HighlightNumbers(InventoryText.Get("SOCKET_LOCKED_BODY", need)),
                    default, InventoryText.Get("DETAIL_LEVEL", need), "", false);
            }
            else if (rune == null || rune.IsEmpty)
            {
                rows[i].Bind(SocketRowState.Empty, InventoryText.Get("POPUP_SOCKET_EMPTY", i + 1),
                    InventoryText.Get("POPUP_PICK_RUNE"), default, "", "", i == _socketIndex);
            }
            else
            {
                var data = cfg.GetRune(rune.runeId);
                var def = Def(rune);
                rows[i].Bind(SocketRowState.Filled, data != null ? data.itemName : InventoryText.Get("RUNE_UNKNOWN"),
                    InventoryUIUtil.HighlightNumbers(InventoryUIUtil.RuneSummary(rune, data)),
                    InventoryUIUtil.RarityColor(def != null ? def.rarity : Rarity.Common), "",
                    InventoryUIUtil.HighlightNumbers(InventoryText.Get("POPUP_COST", cost)), false);
                bool active = false;
                foreach (var entry in activeRunes) if (entry.SocketIndex == i) { active = true; break; }
                rows[i].SetRuneVisual(data != null ? data.icon : null, active);
            }
        }
    }

    private void RebuildRuneGrid()
    {
        var slots = _data.Inventory.GetSlots(InventoryCategory.Rune);

        while (_views.Count < slots.Count)
        {
            var view = Instantiate(bagSlotPrefab, bagContent);
            view.kind = SlotKind.Bag;
            view.Clicked += OnRuneClicked;
            view.Highlighted += EnsureVisible;
            _views.Add(view);
        }

        if (_selected != null && _data.Inventory.Find(_selected.instanceId) == null) _selected = null;

        int used = 0;
        for (int i = 0; i < _views.Count; i++)
        {
            bool active = i < slots.Count;
            _views[i].gameObject.SetActive(active);
            if (!active) continue;

            var item = slots[i];
            _views[i].Bind(item, Def(item));
            if (item != null) used++;
        }
        bagCountText.text = $"{used} / {slots.Count}";

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)bagContent);
        PlaceSelectionFrame();
    }

    private void PlaceSelectionFrame()
    {
        if (selectionFrame == null) return;
        selectionFrame.gameObject.SetActive(false);
        if (_selected == null) return;
        foreach (var view in _views)
        {
            if (!view.gameObject.activeSelf || view.Item == null || view.Item.InstanceId != _selected.instanceId) continue;
            // A child of the selected slot follows both scrolling and layout rebuilds.
            selectionFrame.SetParent(view.transform, false);
            selectionFrame.anchorMin = Vector2.zero;
            selectionFrame.anchorMax = Vector2.one;
            selectionFrame.offsetMin = selectionFrame.offsetMax = Vector2.zero;
            selectionFrame.SetAsLastSibling();
            selectionFrame.gameObject.SetActive(true);
            break;
        }
    }

    private void EnsureVisible(InventorySlotView view)
    {
        if (bagScroll == null || bagScroll.viewport == null || bagScroll.content == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(bagScroll.content);
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(bagScroll.viewport, view.transform);
        var rect = bagScroll.viewport.rect;
        var position = bagScroll.content.anchoredPosition;
        if (bounds.min.y < rect.yMin) position.y += rect.yMin - bounds.min.y;
        else if (bounds.max.y > rect.yMax) position.y -= bounds.max.y - rect.yMax;
        position.y = Mathf.Clamp(position.y, 0, Mathf.Max(0, bagScroll.content.rect.height - rect.height));
        bagScroll.StopMovement();
        bagScroll.content.anchoredPosition = position;
    }

    private void RefreshCard(EquipmentInstance weapon)
    {
        ClearRows();

        if (_selected == null)
        {
            cardContent.SetActive(false);
            hintText.gameObject.SetActive(true);
            hintText.text = InventoryText.Get("POPUP_NO_RUNE");
            return;
        }

        cardContent.SetActive(true);
        hintText.gameObject.SetActive(false);

        var cfg = _equip.ProgressConfig;
        var data = cfg != null ? cfg.GetRune(_selected.runeId) : null;
        var def = Def(_selected);
        var rarity = def != null ? def.rarity : Rarity.Common;

        runeName.text = def != null ? (def.itemName ?? "").ToUpper() : InventoryText.Get("DETAIL_UNKNOWN_ITEM");
        rarityBg.color = InventoryUIUtil.RarityColor(rarity);
        rarityText.text = InventoryUIUtil.RarityName(rarity);

        if (qualityGroup != null) qualityGroup.SetActive(data != null);
        if (data != null)
        {
            float quality = RuneResolver.GetQuality(_selected, data);
            if (qualityFill != null) InventoryUIUtil.SetBar(qualityFill, quality);
            if (qualityValue != null) qualityValue.text = $"{quality * 100f:0}%";
            if (qualityGroup == null) AddRow(InventoryText.Get("DETAIL_QUALITY"), $"{quality * 100f:0}%");

            if (data.kind == RuneKind.DamageEffect)
            {
                RuneResolver.TryGetRoll(_selected, RuneData.DurationId, out float duration);
                AddRow(InventoryText.Get("DETAIL_DURATION"), InventoryText.Get("DETAIL_SECONDS", duration.ToString("0.#")), durationIcon);
                RuneResolver.TryGetRoll(_selected, RuneData.DamagePerTickId, out float damage);
                AddRow(InventoryText.Get("DETAIL_EFFECT_DAMAGE", data.effectType), $"{damage:0.#} / {data.tickInterval:0.#}s", data.icon != null ? data.icon : effectIcon);
            }
            else
            {
                foreach (var line in data.statLines)
                {
                    if (!RuneResolver.TryGetRoll(_selected, line.Id, out float v)) continue;
                    AddRow(InventoryUIUtil.StatName(line.stat), line.mode == StatMode.Percent ? $"+{v:0.##}%" : $"+{v:0.##}", IconFor(line.stat));
                }
            }
        }

        string description = def != null ? def.itemDescription : "";
        descGroup.SetActive(!string.IsNullOrEmpty(description));
        descText.text = InventoryUIUtil.HighlightNumbers(description);

        RuneData other = null;
        bool replaces = false;
        bool conflict = data != null && TryGetConflict(weapon, data, out other, out replaces);
        warnGroup.SetActive(conflict);
        if (conflict) warnText.text = InventoryText.Get(replaces ? "POPUP_CONFLICT_REPLACES" : "POPUP_CONFLICT_WEAKER", other.itemName);
    }

    // Same exclusive group already on this weapon: only the stronger rune is active (ties: lower socket wins).
    private bool TryGetConflict(EquipmentInstance weapon, RuneData selectedData, out RuneData other, out bool replaces)
    {
        other = null;
        replaces = false;
        var cfg = _equip.ProgressConfig;
        var sockets = _equip.Sockets;
        if (cfg == null || sockets == null || _selected == null) return false;
        float quality = RuneResolver.GetQuality(_selected, selectedData);
        float strongestQuality = float.NegativeInfinity;
        int strongestIndex = -1;

        for (int i = 0; i < EquipmentInstance.SocketCount; i++)
        {
            if (i == _socketIndex || !sockets.IsSocketUnlocked(weapon, i)) continue;
            var rune = weapon.GetSocket(i);
            if (rune == null || rune.IsEmpty) continue;
            var data = cfg.GetRune(rune.runeId);
            if (data == null || data.GroupKey != selectedData.GroupKey) continue;
            float candidate = RuneResolver.GetQuality(rune, data);
            // Compare every candidate; the first matching rune may already be inactive.
            if (strongestIndex >= 0 && candidate <= strongestQuality) continue;
            strongestQuality = candidate;
            strongestIndex = i;
            other = data;
        }
        if (strongestIndex < 0) return false;
        replaces = quality > strongestQuality || (Mathf.Approximately(quality, strongestQuality) && _socketIndex < strongestIndex);
        return true;
    }

    private void RefreshButtons(EquipmentInstance weapon)
    {
        var sockets = _equip.Sockets;
        bool canInsert = _selected != null && sockets != null
                         && sockets.IsSocketUnlocked(weapon, _socketIndex)
                         && (weapon.GetSocket(_socketIndex) == null || weapon.GetSocket(_socketIndex).IsEmpty);
        insertButton.interactable = canInsert;
        gemText.text = _data.Currency.Gem().ToString("N0");
    }

    // ---------------------------------------------------------------- actions

    private void OnRowClicked(RuneSocketRowView row)
    {
        var weapon = _equip.GetEquipped(_weaponSlot);
        var sockets = _equip.Sockets;
        if (weapon == null || sockets == null) return;

        int i = row.index;
        if (!sockets.IsSocketUnlocked(weapon, i))
        {
            Message?.Invoke(InventoryText.Get("TOAST_SOCKET_UNLOCKS", UnlockLevel(i)));
            return;
        }

        if (weapon.GetSocket(i) == null || weapon.GetSocket(i).IsEmpty)
        {
            _socketIndex = i;
            Refresh();
        }
    }

    private void OnRuneClicked(InventorySlotView slot)
    {
        if (!(slot.Item is RuneInstance rune)) return;
        _selected = rune;
        Refresh();
    }

    private void OnInsertClicked()
    {
        var weapon = _equip.GetEquipped(_weaponSlot);
        var sockets = _equip.Sockets;
        if (weapon == null || sockets == null || _selected == null) return;

        var result = sockets.TryInsert(weapon, _socketIndex, _selected);
        Message?.Invoke(InventoryUIUtil.SocketMessage(result, "TOAST_RUNE_SOCKETED"));
        if (result != SocketResult.Success) return;

        _selected = null;
        _socketIndex = NextEmptySocket(weapon, _socketIndex);
        Refresh();
    }

    // First click arms the button ("Confirm"), second click within 3 seconds removes the rune and spends the Gem.
    private void OnRemoveClicked(RuneSocketRowView row)
    {
        var weapon = _equip.GetEquipped(_weaponSlot);
        var sockets = _equip.Sockets;
        if (weapon == null || sockets == null) return;

        int i = row.index;
        if (_armedIndex == i && Time.unscaledTime < _armedUntil)
        {
            _armedIndex = -1;
            var result = sockets.TryRemove(weapon, i);
            Message?.Invoke(InventoryUIUtil.SocketMessage(result, "TOAST_RUNE_REMOVED"));
            if (result == SocketResult.Success) _socketIndex = i;
            Refresh();
            return;
        }

        Disarm();
        _armedIndex = i;
        _armedUntil = Time.unscaledTime + 3f;
        row.SetRemoveArmed(true);
    }

    private void Disarm()
    {
        if (_armedIndex >= 0 && _armedIndex < rows.Length) rows[_armedIndex].SetRemoveArmed(false);
        _armedIndex = -1;
    }

    private int NextEmptySocket(EquipmentInstance weapon, int from)
    {
        var sockets = _equip.Sockets;
        for (int step = 1; step <= EquipmentInstance.SocketCount; step++)
        {
            int i = (from + step) % EquipmentInstance.SocketCount;
            if (sockets.IsSocketUnlocked(weapon, i) && (weapon.GetSocket(i) == null || weapon.GetSocket(i).IsEmpty)) return i;
        }
        return from;
    }

    // ---------------------------------------------------------------- helpers

    private int UnlockLevel(int socketIndex)
    {
        var cfg = _equip.ProgressConfig;
        return cfg != null && socketIndex < cfg.socketUnlockLevels.Length ? cfg.socketUnlockLevels[socketIndex] : 0;
    }

    private ItemData Def(IInventoryItem item)
    {
        string id = null;
        switch (item)
        {
            case EquipmentInstance e: id = e.definitionId; break;
            case ArtifactInstance a: id = a.definitionId; break;
            case RuneInstance r: id = r.runeId; break;
        }
        return string.IsNullOrEmpty(id) ? null : _data.GetDefinition<ItemData>(id);
    }

    private Sprite IconFor(BonusStat stat)
    {
        if (statIcons != null) foreach (var entry in statIcons) if (entry.stat == stat) return entry.icon;
        return null;
    }

    private void AddRow(string label, string value, Sprite sprite = null)
    {
        var row = Instantiate(statRowPrefab, rowsRoot);
        var labelText = row.transform.Find("Label")?.GetComponent<TMP_Text>();
        var valueText = row.transform.Find("Value")?.GetComponent<TMP_Text>();
        var tierText = row.transform.Find("Tier")?.GetComponent<TMP_Text>();
        if (labelText != null) labelText.text = label;
        if (valueText != null) valueText.text = value;
        if (tierText != null) tierText.gameObject.SetActive(false);
        var image = row.transform.Find("Icon")?.GetComponent<Image>();
        if (image != null) { image.sprite = sprite; image.enabled = sprite != null; }
    }

    private void ClearRows()
    {
        for (int i = rowsRoot.childCount - 1; i >= 0; i--)
        {
            var child = rowsRoot.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }
}
