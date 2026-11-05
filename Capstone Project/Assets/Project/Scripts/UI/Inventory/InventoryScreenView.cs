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
    [SerializeField] private RuneSocketPopup popup;

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

    private float _toastUntil;
    private GameObject _returnSelection;
    private readonly Dictionary<Selectable, bool> _mainStates = new Dictionary<Selectable, bool>();

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
        if (popup != null)
        {
            popup.Message += Toast;
            popup.Closed += OnPopupClosed;
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
        SetMainInteractable(true);
        if (popup != null) popup.Close();

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
        StopAllCoroutines();
        if (popup != null) popup.Close();
        SetMainInteractable(true);
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
        if (isActiveAndEnabled && (popup == null || !popup.IsOpen) && EventSystem.current != null && meleeSlot != null)
            EventSystem.current.SetSelectedGameObject(meleeSlot.gameObject);
    }

    private void Update()
    {
        if (toastText.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
            toastText.gameObject.SetActive(false);

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame) Back();
        if (popup != null && popup.IsOpen) return;
        if (keyboard.yKey.wasPressedThisFrame) ToggleStats();
    }

    public void ToggleStats()
    {
        bool show = !statsPanel.activeSelf;
        statsPanel.SetActive(show);
        detailPanel.SetActive(!show);
    }

    private void Back()
    {
        if (popup != null && popup.IsOpen) popup.Close();
        else if (statsPanel.activeSelf) ToggleStats();
        else onBack?.Invoke();
    }

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
        if (popup != null && popup.IsOpen) SetMainInteractable(false);
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

        detail.Show(rune, Def(rune), InventoryText.Get("SOCKET_HEADER", i + 1), _equip);
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
            OpenPopupForRune(rune);
            return;
        }

        if (!_equip.Equip(slot.Item)) Toast(InventoryText.Get("TOAST_CANNOT_EQUIP"));
    }

    private void OnPipClicked(SocketPipView pip)
    {
        if (_equip.GetEquipped(pip.weaponSlot) == null) return;
        OpenPopup(pip.weaponSlot, pip.socketIndex, null);
    }

    // Clicking a rune in the bag opens the socket popup with that rune preselected on the first free socket.
    private void OpenPopupForRune(RuneInstance rune)
    {
        var sockets = _equip.Sockets;
        if (sockets == null) return;

        foreach (var slot in new[] { EquipSlot.Melee, EquipSlot.Ranged })
        {
            var weapon = _equip.GetEquipped(slot);
            if (weapon == null) continue;

            for (int i = 0; i < EquipmentInstance.SocketCount; i++)
            {
                var socket = weapon.GetSocket(i);
                if (!sockets.IsSocketUnlocked(weapon, i) || socket is { IsEmpty: false }) continue;
                OpenPopup(slot, i, rune);
                return;
            }
        }

        Toast(InventoryText.Get("TOAST_NO_FREE_SOCKET"));
    }

    private void OpenPopup(EquipSlot weaponSlot, int socketIndex, RuneInstance preselect)
    {
        if (popup == null) return;
        if (!popup.IsOpen && EventSystem.current != null)
            _returnSelection = EventSystem.current.currentSelectedGameObject;
        SetMainInteractable(false); // keyboard navigation must stay inside the popup
        popup.Open(_data, _equip, weaponSlot, socketIndex, preselect);
    }

    private void SetMainInteractable(bool value)
    {
        if (value)
        {
            foreach (var pair in _mainStates)
                if (pair.Key != null) pair.Key.interactable = pair.Value;
            _mainStates.Clear();
            return;
        }
        foreach (var selectable in GetComponentsInChildren<Selectable>(true))
        {
            if (popup != null && selectable.transform.IsChildOf(popup.transform)) continue;
            if (!_mainStates.ContainsKey(selectable)) _mainStates.Add(selectable, selectable.interactable);
            selectable.interactable = false;
        }
    }

    private void OnPopupClosed()
    {
        if (this == null) return;
        SetMainInteractable(true);
        if (isActiveAndEnabled) StartCoroutine(RestoreSelection());
    }

    private IEnumerator RestoreSelection()
    {
        yield return null;
        if (!isActiveAndEnabled || (popup != null && popup.IsOpen) || EventSystem.current == null) yield break;
        var previous = _returnSelection != null ? _returnSelection.GetComponent<Selectable>() : null;
        if (previous != null && previous.isActiveAndEnabled && previous.IsInteractable())
            EventSystem.current.SetSelectedGameObject(previous.gameObject);
        else if (meleeSlot != null && meleeSlot.gameObject.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(meleeSlot.gameObject);
        _returnSelection = null;
    }

    private void Toast(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        _toastUntil = Time.unscaledTime + 2.5f;
    }
}