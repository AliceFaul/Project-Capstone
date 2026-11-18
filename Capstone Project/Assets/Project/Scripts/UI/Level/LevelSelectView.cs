using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Level select screen of the lobby (map table / gate). Opens when a LevelGate is clicked: pick a level on the map, pick a difficulty
// (A / D or click), compare the recommended combat power with yours, look at the possible rewards and start the run.
// Esc closes it. Lives on an always-active canvas and listens to LevelGate.OpenRequested.
public class LevelSelectView : MonoBehaviour
{
    private static readonly Color Ok = new Color(0.31f, 0.78f, 0.45f);
    private static readonly Color Warn = new Color(1f, 0.70f, 0.36f);
    private static readonly Color RingSelected = new Color(0.94f, 0.50f, 0.16f);
    private static readonly Color RingDone = new Color(0.31f, 0.70f, 0.42f);
    private static readonly Color RingOpen = new Color(0.62f, 0.71f, 0.75f);
    private static readonly Color RingLocked = new Color(0.30f, 0.40f, 0.45f);

    [Header("Root")]
    [SerializeField] private GameObject screenRoot;
    [Tooltip("Lobby HUD and other canvases hidden while the screen is open (their Canvas component is switched off).")]
    [SerializeField] private GameObject[] hideWhileOpen;

    [Header("Map")]
    [SerializeField] private Image mapImage;
    [SerializeField] private RectTransform markerRoot;
    [SerializeField] private LevelMarkerView markerPrefab;

    [Header("Level info")]
    [SerializeField] private Image headerImage;
    [SerializeField] private TMP_Text badgeText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Difficulty")]
    [SerializeField] private TMP_Text selectedDifficultyText;
    [SerializeField] private RectTransform trackFill;
    [SerializeField] private Button[] difficultyButtons;
    [SerializeField] private Image[] difficultyRings;
    [SerializeField] private TMP_Text[] difficultyLabels;
    [SerializeField] private TMP_Text[] difficultyHints;

    [Header("Power and enemies")]
    [SerializeField] private TMP_Text recommendedText;
    [SerializeField] private TMP_Text enemyText;
    [SerializeField] private TMP_Text lootText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text powerDeltaText;

    [Header("Rewards")]
    [SerializeField] private RewardSlotView rewardSlotPrefab;
    [SerializeField] private GameObject gearGroup;
    [SerializeField] private Transform gearRoot;
    [SerializeField] private GameObject runeGroup;
    [SerializeField] private Transform runeRoot;
    [SerializeField] private GameObject artifactGroup;
    [SerializeField] private Transform artifactRoot;
    [SerializeField] private TMP_Text rewardNameText;

    [Header("Start and progress")]
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text bestBadgeText;
    [SerializeField] private TMP_Text bestCountText;
    [SerializeField] private TMP_Text toastText;

    private PlayerController _player;
    private PlayerDataConfig _data;
    private LevelCatalog _catalog;
    private CombatPowerConfig _powerConfig;
    private int _levelIndex;
    private int _difficulty;
    private int _power;
    private float _toastUntil;
    private bool _starting;

    private readonly List<LevelMarkerView> _markers = new List<LevelMarkerView>();
    private readonly List<RewardSlotView> _rewardSlots = new List<RewardSlotView>();
    private readonly List<Canvas> _hiddenCanvases = new List<Canvas>();

    public bool IsOpen => screenRoot != null && screenRoot.activeSelf;

    private void Awake()
    {
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            int index = i;
            difficultyButtons[i].onClick.AddListener(() => SelectDifficulty(index));
        }
        startButton.onClick.AddListener(OnStartClicked);
        screenRoot.SetActive(false);
    }

    private void OnEnable() => LevelGate.OpenRequested += Open;

    private void OnDisable()
    {
        LevelGate.OpenRequested -= Open;
        Close();
    }

    // ---------------------------------------------------------------- open / close

    private void Open(LevelGate gate, PlayerController player)
    {
        if (_starting || IsOpen || gate == null || gate.Catalog == null || gate.Catalog.Levels.Count == 0) return;

        _catalog = gate.Catalog;
        _powerConfig = gate.PowerConfig;
        _data = player != null && player.PlayerRuntime != null ? player.PlayerRuntime.Config : null;
        _power = CombatPower.Calculate(_data, _powerConfig);

        // start on the level and difficulty of the last run, else the first level on the highest difficulty you can pick
        _levelIndex = RunSettings.IsSet ? _catalog.IndexOf(RunSettings.Level) : -1;
        if (_levelIndex < 0)
        {
            for (int i = 0; i < _catalog.Levels.Count; i++)
            {
                if (_catalog.Levels[i] == null) continue;
                _levelIndex = i;
                break;
            }
        }
        if (_levelIndex < 0)
        {
            Debug.LogWarning("[LevelSelect] LevelCatalog contains no assigned LevelData.", _catalog);
            return;
        }
        var level = _catalog.Levels[_levelIndex];
        _difficulty = RunSettings.IsSet && RunSettings.Level == level
            ? RunSettings.DifficultyIndex
            : LevelUnlock.HighestUnlockedDifficulty(level, _data);

        // Bind while the screen is closed, before taking control of the player and HUD.
        toastText.gameObject.SetActive(false);
        BuildMarkers(level);
        RefreshDetails(level);

        _player = player;
        if (_player != null) _player.CmdCombatLocked(true);
        HideOthers();
        screenRoot.SetActive(true);
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    public void Close()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        if (IsOpen) screenRoot.SetActive(false);
        RestoreOthers();
        if (_player != null)
        {
            _player.CmdCombatLocked(false);
            _player = null;
        }
    }

    private void HideOthers()
    {
        _hiddenCanvases.Clear();
        if (hideWhileOpen == null) return;

        foreach (var root in hideWhileOpen)
        {
            if (root == null) continue;
            foreach (var canvas in root.GetComponentsInChildren<Canvas>())
            {
                if (!canvas.enabled) continue;
                canvas.enabled = false;
                _hiddenCanvases.Add(canvas);
            }
        }
    }

    private void RestoreOthers()
    {
        foreach (var canvas in _hiddenCanvases)
        {
            if (canvas != null) canvas.enabled = true;
        }
        _hiddenCanvases.Clear();
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale locale) => RefreshAll();

    private void Update()
    {
        if (!IsOpen) return;

        if (toastText.gameObject.activeSelf && Time.unscaledTime > _toastUntil) toastText.gameObject.SetActive(false);

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }
        if (keyboard.aKey.wasPressedThisFrame) StepDifficulty(-1);
        if (keyboard.dKey.wasPressedThisFrame) StepDifficulty(1);
    }

    // ---------------------------------------------------------------- map

    private void BuildMarkers(LevelData selectedLevel)
    {
        foreach (var marker in _markers)
        {
            if (marker == null) continue;
            marker.Clicked -= OnMarkerClicked;
            marker.gameObject.SetActive(false);
            Destroy(marker.gameObject);
        }
        _markers.Clear();

        foreach (var level in _catalog.Levels)
        {
            if (level == null) continue;

            var marker = Instantiate(markerPrefab, markerRoot);
            _markers.Add(marker);
            var rect = (RectTransform)marker.transform;
            rect.anchorMin = rect.anchorMax = level.mapPosition;
            rect.anchoredPosition = Vector2.zero;
            marker.Bind(level, LevelUnlocked(level), level == selectedLevel);
            marker.Clicked += OnMarkerClicked;
        }
    }

    private void OnMarkerClicked(LevelMarkerView marker)
    {
        if (_starting || !IsOpen) return;
        int index = _catalog.IndexOf(marker.Level);
        if (index < 0 || index == _levelIndex) return;

        _levelIndex = index;
        _difficulty = Mathf.Min(_difficulty, HighestSelectable(marker.Level));
        RefreshAll();
    }

    // ---------------------------------------------------------------- refresh

    private LevelData CurrentLevel => _catalog.Levels[_levelIndex];

    private bool LevelUnlocked(LevelData level) => LevelUnlock.IsLevelUnlocked(_catalog, level, _data);

    private int HighestSelectable(LevelData level) => LevelUnlocked(level) ? LevelUnlock.HighestUnlockedDifficulty(level, _data) : 0;

    private void RefreshAll()
    {
        var level = CurrentLevel;
        foreach (var marker in _markers) marker.Bind(marker.Level, LevelUnlocked(marker.Level), marker.Level == level);
        RefreshDetails(level);
    }

    private void RefreshDetails(LevelData level)
    {
        bool unlocked = LevelUnlocked(level);
        headerImage.sprite = level.preview;
        headerImage.color = level.preview != null ? Color.white : new Color(0.23f, 0.42f, 0.29f);
        badgeText.text = InventoryText.Get("LEVEL_BADGE", _levelIndex + 1);
        nameText.text = level.GetName().ToUpper();
        descriptionText.text = unlocked ? level.GetDescription() : InventoryText.Get("LEVEL_LOCKED_LEVEL");

        RefreshDifficulty(level, unlocked);
        RefreshRewards(level);
        RefreshProgress(level);
    }

    private void RefreshDifficulty(LevelData level, bool levelUnlocked)
    {
        int count = VisibleDifficultyCount(level);
        int highest = HighestSelectable(level);
        var progress = _data != null ? _data.GetLevelProgress(level.id) : null;
        int cleared = progress?.highestDifficultyCleared ?? -1;

        _difficulty = Mathf.Clamp(_difficulty, 0, Mathf.Max(0, count - 1));

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            bool has = i < count;
            difficultyButtons[i].gameObject.SetActive(has);
            if (!has) continue;

            bool open = levelUnlocked && i <= highest;
            bool selected = i == _difficulty;

            difficultyRings[i].color = selected ? RingSelected : (!open ? RingLocked : (i <= cleared ? RingDone : RingOpen));
            difficultyRings[i].transform.localScale = Vector3.one * (selected ? 1.3f : 1f);
            difficultyLabels[i].text = DifficultyName(level, i);
            difficultyLabels[i].color = selected ? RingSelected : (open ? Color.white : RingOpen);
            difficultyHints[i].gameObject.SetActive(!open && levelUnlocked && i > 0);
            if (!open && i > 0) difficultyHints[i].text = InventoryText.Get("LEVEL_CLEAR_FIRST", DifficultyName(level, i - 1));
        }

        trackFill.anchorMin = Vector2.zero;
        // The builder fixes each button's position on a track spanning all three slots.
        float fill = difficultyButtons.Length > 1 ? _difficulty / (difficultyButtons.Length - 1f) : 0f;
        trackFill.anchorMax = new Vector2(fill, 1f);
        trackFill.offsetMin = trackFill.offsetMax = Vector2.zero;

        var settings = count > 0 ? level.GetDifficulty(_difficulty) : null;
        selectedDifficultyText.text = DifficultyName(level, _difficulty);
        powerText.text = _power.ToString();
        startButton.interactable = settings != null && levelUnlocked && _difficulty <= highest && !_starting;
        if (settings == null)
        {
            recommendedText.text = "";
            enemyText.text = "";
            lootText.text = "";
            powerDeltaText.text = "";
            return;
        }

        recommendedText.text = InventoryUIUtil.HighlightNumbers(InventoryText.Get("LEVEL_RECOMMENDED", settings.recommendedPower));
        enemyText.text = InventoryUIUtil.HighlightNumbers(InventoryText.Get("LEVEL_ENEMIES",
            settings.enemyHealthMultiplier.ToString("0.##"), settings.enemyDamageMultiplier.ToString("0.##")));
        lootText.text = InventoryText.Get(settings.lootDifficulty > 0 ? "LEVEL_LOOT_BETTER" : "LEVEL_LOOT_STANDARD");

        int difference = _power - settings.recommendedPower;
        powerDeltaText.text = difference >= 0 ? InventoryText.Get("LEVEL_POWER_READY") : InventoryText.Get("LEVEL_POWER_BELOW", -difference);
        powerDeltaText.color = difference >= 0 ? Ok : Warn;
    }

    private void RefreshRewards(LevelData level)
    {
        foreach (var slot in _rewardSlots)
        {
            if (slot == null) continue;
            slot.Hovered -= OnRewardHovered;
            slot.gameObject.SetActive(false);
            Destroy(slot.gameObject);
        }
        _rewardSlots.Clear();
        rewardNameText.text = "";

        var settings = level.GetDifficulty(_difficulty);
        int gear = 0, runes = 0, artifacts = 0;

        if (settings != null && settings.rewardPreview != null)
        {
            var seen = new HashSet<ItemData>();
            foreach (var entry in settings.rewardPreview.entries)
            {
                var item = entry?.item;
                if (item == null || !seen.Add(item)) continue;

                Transform parent;
                if (item is EquipmentData) { parent = gearRoot; gear++; }
                else if (item is RuneData) { parent = runeRoot; runes++; }
                else if (item is ArtifactData) { parent = artifactRoot; artifacts++; }
                else continue;

                var slot = Instantiate(rewardSlotPrefab, parent);
                _rewardSlots.Add(slot);
                slot.Bind(item);
                slot.Hovered += OnRewardHovered;
            }
        }

        gearGroup.SetActive(gear > 0);
        runeGroup.SetActive(runes > 0);
        artifactGroup.SetActive(artifacts > 0);
    }

    private void OnRewardHovered(RewardSlotView slot, bool hovered) => rewardNameText.text = hovered && slot.Item != null ? slot.Item.itemName : "";

    private void RefreshProgress(LevelData level)
    {
        var progress = _data != null ? _data.GetLevelProgress(level.id) : null;
        if (progress == null || progress.highestDifficultyCleared < 0)
        {
            bestBadgeText.text = "-";
            bestCountText.text = InventoryText.Get("LEVEL_NOT_CLEARED");
            return;
        }

        bestBadgeText.text = DifficultyName(level, progress.highestDifficultyCleared);
        bestCountText.text = InventoryUIUtil.HighlightNumbers(InventoryText.Get("LEVEL_CLEARED_TIMES", progress.timesCompleted));
    }

    private static string DifficultyName(LevelData level, int index)
    {
        var settings = level.GetDifficulty(index);
        return settings != null ? InventoryText.Get("DIFFICULTY_" + settings.difficulty.ToString().ToUpper()) : "";
    }

    private int VisibleDifficultyCount(LevelData level) =>
        Mathf.Min(level.difficulties?.Length ?? 0, difficultyButtons.Length);

    private void SelectDifficulty(int index)
    {
        if (_starting || !IsOpen) return;
        var level = CurrentLevel;
        if (index < 0 || index >= VisibleDifficultyCount(level) || !LevelUnlocked(level) || index > HighestSelectable(level)) return;

        _difficulty = index;
        RefreshDifficulty(level, true);
        RefreshRewards(level);
    }

    private void StepDifficulty(int step)
    {
        var level = CurrentLevel;
        int count = VisibleDifficultyCount(level);
        if (count == 0) return;
        int target = Mathf.Clamp(_difficulty + step, 0, count - 1);
        if (target != _difficulty) SelectDifficulty(target);
    }

    private async void OnStartClicked()
    {
        try
        {
            if (_starting || !IsOpen || !startButton.interactable) return;
            _starting = true;
            startButton.interactable = false;

            try
            {
                if (await LevelStarter.Start(CurrentLevel, _difficulty, _data, _catalog)) return;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _starting = false;
            }

            // Scene loading can finish after the lobby canvas was closed or destroyed.
            if (this == null || !IsOpen) return;
            Toast(InventoryText.Get("LEVEL_CANNOT_START"));
            RefreshDifficulty(CurrentLevel, LevelUnlocked(CurrentLevel));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void Toast(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        _toastUntil = Time.unscaledTime + 2.5f;
    }
}