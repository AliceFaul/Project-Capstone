using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Capstone.Inventory;

// Modal popup for the sockets of ONE equipped weapon: shows its 3 sockets, lets the player pick a rune from the rune bag,
// preview it (quality, description, same-group warning) and insert it, or remove a socketed rune (two clicks, costs Gem).
// Esc is handled by InventoryScreenView (it calls Close()).
public class RuneSocketPopup : MonoBehaviour
{
    [Header("Backdrop")]
    [SerializeField] private Image backdrop; // dim overlay behind the panel (the blurred snapshot sits just below it)
    [SerializeField] private UIBlurBackdrop blur;

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
        _data = data;
        _equip = equip;
        _weaponSlot = weaponSlot;
        _socketIndex = Mathf.Clamp(socketIndex, 0, EquipmentInstance.SocketCount - 1);
        _selected = preselect;
        _armedIndex = -1;

        gameObject.SetActive(true); // OnEnable refreshes
        if (wasOpen) Refresh();
    }

    public void Close() => gameObject.SetActive(false);

    // Called by InventoryScreenView before Open(): snapshots and blurs the screen as it looks right now (no-op without a UIBlurBackdrop).
    public IEnumerator PrepareBackdrop()
    {
        if (blur != null) yield return blur.Capture();
    }

    private void OnEnable()
    {
        if (_data == null || _equip == null) return;

        _equip.OnEquipmentStatsChanged += OnStatsChanged;
        _equip.OnLoadoutReloaded += Refresh;
        _data.Inventory.OnBagChanged += OnBagChanged;
        _data.Currency.OnCurrencyChanged += OnCurrencyChanged;
        _subscribed = true;

        Refresh();
        StartCoroutine(FocusFirst());
    }

    private void OnDisable()
    {
        if (_subscribed)
        {
            _subscribed = false;
            _equip.OnEquipmentStatsChanged -= OnStatsChanged;
            _equip.OnLoadoutReloaded -= Refresh;
            _data.Inventory.OnBagChanged -= OnBagChanged;
            _data.Currency.OnCurrencyChanged -= OnCurrencyChanged;
        }
        if (blur != null) blur.Release();
        Closed?.Invoke();
    }

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
            if (!view.gameObject.activeSelf) continue;
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

        weaponIcon.sprite = hasSprite ? def.icon : null;
        weaponIcon.color = hasSprite ? Color.white : InventoryUIUtil.RarityColor(rarity);
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
        bool shown = false;
        if (_selected != null)
        {
            foreach (var view in _views)
            {
                if (!view.gameObject.activeSelf || view.Item == null || view.Item.InstanceId != _selected.instanceId) continue;
                selectionFrame.gameObject.SetActive(true);
                selectionFrame.position = ((RectTransform)view.transform).position;
                shown = true;
                break;
            }
        }

        if (!shown) selectionFrame.gameObject.SetActive(false);
        selectionFrame.SetAsFirstSibling(); // behind the slots: only the outer ring shows
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

        if (data != null)
        {
            AddRow(InventoryText.Get("DETAIL_QUALITY"), $"{RuneResolver.GetQuality(_selected, data) * 100f:0}%");

            if (data.kind == RuneKind.DamageEffect)
            {
                RuneResolver.TryGetRoll(_selected, RuneData.DurationId, out float duration);
                AddRow(InventoryText.Get("DETAIL_DURATION"), InventoryText.Get("DETAIL_SECONDS", duration.ToString("0.#")));
            }
            else
            {
                foreach (var line in data.statLines)
                {
                    if (!RuneResolver.TryGetRoll(_selected, line.Id, out float v)) continue;
                    AddRow(InventoryUIUtil.StatName(line.stat), line.mode == StatMode.Percent ? $"+{v:0.##}%" : $"+{v:0.##}");
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
        float quality = RuneResolver.GetQuality(_selected, selectedData);

        for (int i = 0; i < EquipmentInstance.SocketCount; i++)
        {
            if (i == _socketIndex) continue;

            var rune = weapon.GetSocket(i);
            if (rune == null || rune.IsEmpty) continue;

            var data = cfg.GetRune(rune.runeId);
            if (data == null || data.GroupKey != selectedData.GroupKey) continue;

            float otherQuality = RuneResolver.GetQuality(rune, data);
            other = data;
            replaces = quality > otherQuality || (Mathf.Approximately(quality, otherQuality) && _socketIndex < i);
            return true;
        }
        return false;
    }

    private void RefreshButtons(EquipmentInstance weapon)
    {
        var sockets = _equip.Sockets;
        bool canInsert = _selected != null && sockets != null
                         && sockets.IsSocketUnlocked(weapon, _socketIndex)
                         && weapon.GetSocket(_socketIndex).IsEmpty;
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

        if (weapon.GetSocket(i).IsEmpty)
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
            if (sockets.IsSocketUnlocked(weapon, i) && weapon.GetSocket(i).IsEmpty) return i;
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

    private void AddRow(string label, string value)
    {
        var row = Instantiate(statRowPrefab, rowsRoot);
        var texts = row.GetComponentsInChildren<TMP_Text>();
        texts[0].text = label;
        texts[1].text = value;
        texts[2].text = "";
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