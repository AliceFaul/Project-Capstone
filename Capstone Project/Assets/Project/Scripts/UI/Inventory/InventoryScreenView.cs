using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Project.Capstone.Inventory;

public class InventoryScreenView : MonoBehaviour
{
    private static readonly InventoryCategory[] TabCategories =
    {
        InventoryCategory.Melee, InventoryCategory.Ranged, InventoryCategory.Armor,
        InventoryCategory.Rune, InventoryCategory.Artifact
    };
    private const int RuneTab = 3;

    [Header("Header")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private RectTransform expFill;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text gemText;

    [Header("Equipment")]
    [SerializeField] private InventorySlotView meleeSlot;
    [SerializeField] private InventorySlotView rangedSlot;
    [SerializeField] private InventorySlotView armorSlot;
    [SerializeField] private InventorySlotView[] artifactSlots;

    [Header("Bag")]
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private GameObject[] tabUnderlines;
    [SerializeField] private Transform bagContent;
    [SerializeField] private InventorySlotView bagSlotPrefab;
    [SerializeField] private TMP_Text bagCountText;

    [Header("Panels")]
    [SerializeField] private InventoryDetailView detail;
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private TMP_Text toastText;

    public UnityEvent onBack;

    // True when equipment, sockets or bags changed since the last ClearChanges() - the controller saves on close.
    public bool HasChanges { get; private set; }
    public void ClearChanges() => HasChanges = false;

    private PlayerDataConfig _data;
    private EquipmentManager _equip;
    private bool _subscribed;

    private readonly List<InventorySlotView> _bagViews = new List<InventorySlotView>();
    private readonly List<SocketPipView> _pips = new List<SocketPipView>();
    private int _tab;
    private object _focus;

    private EquipmentInstance _pendingWeapon;   // socket chosen by clicking an empty pip
    private int _pendingIndex = -1;
    private EquipmentInstance _removeWeapon;    // rune removal needs a second click (costs Gem)
    private int _removeIndex = -1;
    private float _removeUntil;
    private float _toastUntil;

    private void Awake()
    {
        GetComponentsInChildren(true, _pips);

        foreach (var s in EquipSlots())
        {
            s.Highlighted += OnSlotHighlighted;
            s.Clicked += OnSlotClicked;
        }
        foreach (var p in _pips)
        {
            p.Highlighted += OnPipHighlighted;
            p.Clicked += OnPipClicked;
        }
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabButtons[i].onClick.AddListener(() => SelectTab(index));
        }
    }

    private IEnumerable<InventorySlotView> EquipSlots()
    {
        yield return meleeSlot;
        yield return rangedSlot;
        yield return armorSlot;
        foreach (var a in artifactSlots) yield return a;
    }

    private bool TryBind()
    {
        if (_data != null && _equip != null) return true;

        var startup = StartupProcessor.Instance;
        if (startup == null || !startup.GetService(out ConfigManager configMg) || !configMg.GetConfig(out PlayerDataConfig data))
        {
            Debug.LogWarning("[InventoryScreen] Config not ready yet (startup not finished).");
            return false;
        }

        if (EquipmentManager.Instance == null)
        {
            Debug.LogWarning("[InventoryScreen] EquipmentManager.Instance is missing.");
            return false;
        }

        _data = data;
        _equip = EquipmentManager.Instance;
        return true;
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        statsPanel.SetActive(false);
        detailPanel.SetActive(true);
        detail.Hide();
        toastText.gameObject.SetActive(false);
        _pendingWeapon = null;
        _removeWeapon = null;

        if (!TryBind()) return;

        _equip.OnEquipmentChanged += OnEquipChanged;
        _equip.OnLoadoutReloaded += RefreshAll;
        _equip.OnEquipmentStatsChanged += OnStatsChanged;
        _data.Inventory.OnBagChanged += OnBagChanged;
        _data.Currency.OnCurrencyChanged += OnCurrencyChanged;
        _data.OnExpChanged += OnExpChanged;
        _data.OnLevelUp += OnLevelUp;
        _data.OnDataApplied += RefreshAll;
        _subscribed = true;

        RefreshAll();
        StartCoroutine(SelectFirst());
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        _focus = null;
        if (!_subscribed) return;
        _subscribed = false;

        if (_equip != null)
        {
            _equip.OnEquipmentChanged -= OnEquipChanged;
            _equip.OnLoadoutReloaded -= RefreshAll;
            _equip.OnEquipmentStatsChanged -= OnStatsChanged;
        }
        if (_data != null)
        {
            _data.Inventory.OnBagChanged -= OnBagChanged;
            _data.Currency.OnCurrencyChanged -= OnCurrencyChanged;
            _data.OnExpChanged -= OnExpChanged;
            _data.OnLevelUp -= OnLevelUp;
            _data.OnDataApplied -= RefreshAll;
        }
    }

    private IEnumerator SelectFirst()
    {
        yield return null;
        if (EventSystem.current != null && meleeSlot != null)
            EventSystem.current.SetSelectedGameObject(meleeSlot.gameObject);
    }

    private void Update()
    {
        if (toastText.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
            toastText.gameObject.SetActive(false);

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.yKey.wasPressedThisFrame) ToggleStats();
        if (keyboard.escapeKey.wasPressedThisFrame) Back();
    }

    public void ToggleStats()
    {
        bool show = !statsPanel.activeSelf;
        statsPanel.SetActive(show);
        detailPanel.SetActive(!show);
    }

    private void Back()
    {
        if (_pendingWeapon != null)
        {
            _pendingWeapon = null;
            Toast(InventoryText.Get("TOAST_SELECT_CANCELLED"));
        }
        else if (statsPanel.activeSelf) ToggleStats();
        else onBack?.Invoke();
    }

    // ---------------------------------------------------------------- refresh

    private void OnLocaleChanged(UnityEngine.Localization.Locale locale) => RefreshAll();

    private void RefreshAll()
    {
        if (_data == null || _equip == null) return;
        RefreshHeader();
        RefreshEquipment();
        RebuildBag();
        RefreshDetail();
    }

    private void RefreshHeader()
    {
        levelText.text = _data.Level.ToString();
        expText.text = $"{_data.CurrentExp:N0} / {_data.ExpToNextLevel:N0}";
        InventoryUIUtil.SetBar(expFill, _data.CurrentExp / Mathf.Max(1f, _data.ExpToNextLevel));
        RefreshCurrency();
    }

    private void RefreshCurrency()
    {
        goldText.text = _data.Currency.Gold().ToString("N0");
        gemText.text = _data.Currency.Gem().ToString("N0");
    }

    private void RefreshEquipment()
    {
        BindEquipped(meleeSlot, _equip.GetEquipped(EquipSlot.Melee));
        BindEquipped(rangedSlot, _equip.GetEquipped(EquipSlot.Ranged));
        BindEquipped(armorSlot, _equip.GetEquipped(EquipSlot.Armor));
        for (int i = 0; i < artifactSlots.Length; i++) BindEquipped(artifactSlots[i], _equip.GetArtifact(i));

        foreach (var pip in _pips) RefreshPip(pip);
    }

    private void BindEquipped(InventorySlotView slot, IInventoryItem item) => slot.Bind(item, Def(item));

    private void RefreshPip(SocketPipView pip)
    {
        var weapon = _equip.GetEquipped(pip.weaponSlot);
        var sockets = _equip.Sockets;
        if (weapon == null || sockets == null)
        {
            pip.SetState(PipState.Hidden, default);
            return;
        }

        if (!sockets.IsSocketUnlocked(weapon, pip.socketIndex))
        {
            pip.SetState(PipState.Locked, default);
            return;
        }

        var rune = weapon.GetSocket(pip.socketIndex);
        if (rune == null || rune.IsEmpty)
        {
            pip.SetState(PipState.Empty, default);
            return;
        }

        var data = Def(rune);
        pip.SetState(PipState.Filled, InventoryUIUtil.RarityColor(data != null ? data.rarity : Rarity.Common));
    }

    private void RebuildBag()
    {
        var category = TabCategories[_tab];
        var slots = _data.Inventory.GetSlots(category);

        while (_bagViews.Count < slots.Count)
        {
            var view = Instantiate(bagSlotPrefab, bagContent);
            view.kind = SlotKind.Bag;
            view.Highlighted += OnSlotHighlighted;
            view.Clicked += OnSlotClicked;
            _bagViews.Add(view);
        }

        int used = 0;
        for (int i = 0; i < _bagViews.Count; i++)
        {
            bool active = i < slots.Count;
            _bagViews[i].gameObject.SetActive(active);
            if (!active) continue;

            var item = slots[i];
            _bagViews[i].Bind(item, Def(item));
            if (item != null) used++;
        }

        bagCountText.text = $"{used} / {slots.Count}";
        for (int i = 0; i < tabUnderlines.Length; i++) tabUnderlines[i].SetActive(i == _tab);
    }

    private void SelectTab(int index)
    {
        _tab = Mathf.Clamp(index, 0, TabCategories.Length - 1);
        RebuildBag();
        RefreshDetail();
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


    // ===== EVENTS =====

    private void OnEquipChanged(EquipmentChangedEventArgs _) { HasChanges = true; RefreshEquipment(); RefreshDetail(); }
    private void OnStatsChanged(EquipSlot _) { HasChanges = true; RefreshEquipment(); RefreshDetail(); }
    private void OnBagChanged(InventoryCategory category) { HasChanges = true; if (category == TabCategories[_tab]) RebuildBag(); RefreshDetail(); }
    private void OnCurrencyChanged(CurrencyType type, int amount) => RefreshCurrency();
    private void OnExpChanged(float exp, float toNext) => RefreshHeader();
    private void OnLevelUp(int level) => RefreshHeader();

    // ===== DETAIL PANEL =====

    private void OnSlotHighlighted(InventorySlotView slot)
    {
        _focus = slot;
        ShowSlotDetail(slot);
    }

    private void OnPipHighlighted(SocketPipView pip)
    {
        _focus = pip;
        ShowPipDetail(pip);
    }

    private void RefreshDetail()
    {
        if (_focus is InventorySlotView slot) ShowSlotDetail(slot);
        else if (_focus is SocketPipView pip) ShowPipDetail(pip);
    }

    private void ShowSlotDetail(InventorySlotView slot)
    {
        if (slot.Item == null)
        {
            detail.Hide(); // nothing to inspect on an empty slot
            return;
        }

        string header = InventoryText.Get(slot.kind == SlotKind.Equipped ? "DETAIL_EQUIPPED" : "DETAIL_IN_BAG");
        detail.Show(slot.Item, slot.Definition, header, _equip);
    }

    private void ShowPipDetail(SocketPipView pip)
    {
        var weapon = _equip.GetEquipped(pip.weaponSlot);
        var sockets = _equip.Sockets;
        if (weapon == null || sockets == null)
        {
            detail.Hide();
            return;
        }

        int i = pip.socketIndex;
        string header = InventoryText.Get("SOCKET_HEADER", i + 1);

        if (!sockets.IsSocketUnlocked(weapon, i))
        {
            detail.ShowInfo(header, InventoryText.Get("SOCKET_LOCKED_TITLE"), InventoryText.Get("SOCKET_LOCKED_BODY", UnlockLevel(i)));
            return;
        }

        var rune = weapon.GetSocket(i);
        if (rune == null || rune.IsEmpty)
        {
            detail.Hide(); // empty socket: nothing to show
            return;
        }

        detail.Show(rune, Def(rune), InventoryText.Get("SOCKET_FILLED_HEADER", i + 1, sockets.GetRemovalCost(weapon)), _equip);
    }

    private int UnlockLevel(int socketIndex)
    {
        var cfg = _equip.ProgressConfig;
        return cfg != null && socketIndex < cfg.socketUnlockLevels.Length ? cfg.socketUnlockLevels[socketIndex] : 0;
    }

    // ===== ACTIONS =====

    private void OnSlotClicked(InventorySlotView slot)
    {
        if (slot.Item == null) return;

        if (slot.kind == SlotKind.Equipped)
        {
            if (!_equip.Unequip(slot.equipSlot, slot.artifactIndex)) Toast(InventoryText.Get("TOAST_BAG_FULL"));
            return;
        }

        if (slot.Item is RuneInstance rune)
        {
            InsertRune(rune);
            return;
        }

        if (!_equip.Equip(slot.Item)) Toast(InventoryText.Get("TOAST_CANNOT_EQUIP"));
    }

    private void InsertRune(RuneInstance rune)
    {
        var sockets = _equip.Sockets;
        if (sockets == null) return;

        EquipmentInstance weapon = _pendingWeapon;
        int index = _pendingIndex;

        // No socket chosen: use the first free unlocked socket (melee first, then ranged).
        if (weapon == null && !TryFindFreeSocket(sockets, out weapon, out index))
        {
            Toast(InventoryText.Get("TOAST_NO_FREE_SOCKET"));
            return;
        }

        var result = sockets.TryInsert(weapon, index, rune);
        _pendingWeapon = null;
        Toast(SocketMessage(result, "TOAST_RUNE_SOCKETED"));
    }

    private bool TryFindFreeSocket(SocketService sockets, out EquipmentInstance weapon, out int index)
    {
        foreach (var slot in new[] { EquipSlot.Melee, EquipSlot.Ranged })
        {
            weapon = _equip.GetEquipped(slot);
            if (weapon == null) continue;

            for (index = 0; index < EquipmentInstance.SocketCount; index++)
            {
                if (sockets.IsSocketUnlocked(weapon, index) && weapon.GetSocket(index).IsEmpty) return true;
            }
        }

        weapon = null;
        index = -1;
        return false;
    }

    private void OnPipClicked(SocketPipView pip)
    {
        var weapon = _equip.GetEquipped(pip.weaponSlot);
        var sockets = _equip.Sockets;
        if (weapon == null || sockets == null) return;

        int i = pip.socketIndex;

        if (!sockets.IsSocketUnlocked(weapon, i))
        {
            Toast(InventoryText.Get("TOAST_SOCKET_UNLOCKS", UnlockLevel(i)));
            return;
        }

        var rune = weapon.GetSocket(i);
        if (rune == null || rune.IsEmpty)
        {
            _pendingWeapon = weapon;
            _pendingIndex = i;
            SelectTab(RuneTab);
            Toast(InventoryText.Get("TOAST_PICK_RUNE"));
            return;
        }

        // Removing costs Gem: require a second click within 3 seconds.
        if (_removeWeapon == weapon && _removeIndex == i && Time.unscaledTime < _removeUntil)
        {
            _removeWeapon = null;
            Toast(SocketMessage(sockets.TryRemove(weapon, i), "TOAST_RUNE_REMOVED"));
            return;
        }

        _removeWeapon = weapon;
        _removeIndex = i;
        _removeUntil = Time.unscaledTime + 3f;
        Toast(InventoryText.Get("TOAST_REMOVE_CONFIRM", sockets.GetRemovalCost(weapon)));
    }

    private static string SocketMessage(SocketResult result, string successKey)
    {
        switch (result)
        {
            case SocketResult.Success: return InventoryText.Get(successKey);
            case SocketResult.NotEnoughGem: return InventoryText.Get("TOAST_NOT_ENOUGH_GEM");
            case SocketResult.InventoryFull: return InventoryText.Get("TOAST_RUNE_BAG_FULL");
            case SocketResult.SocketLocked: return InventoryText.Get("TOAST_SOCKET_LOCKED");
            case SocketResult.SocketOccupied: return InventoryText.Get("TOAST_SOCKET_OCCUPIED");
            case SocketResult.RuneNotInInventory: return InventoryText.Get("TOAST_RUNE_MISSING");
            default: return InventoryText.Get("TOAST_GENERIC_FAIL");
        }
    }

    private void Toast(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        _toastUntil = Time.unscaledTime + 2.5f;
    }
}