using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SettingsScreen : MonoBehaviour
{
    private enum Tab { General, Video, Audio, Controls }

    public static SettingsScreen Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.screenRoot != null && Instance.screenRoot.activeSelf;
    // Frame in which this screen used Esc. Other Esc handlers (main menu "quit?", pause menu) skip that frame.
    public static int EscapeConsumedFrame { get; private set; } = -1;

    public static event Action ChangeNameRequested;
    public static event Action SignOutRequested;
    public static event Action Closed;

    [SerializeField] private GameObject screenRoot;
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private GameObject[] tabUnderlines;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private RectTransform content;
    [SerializeField] private SettingRowView rowPrefab;
    [SerializeField] private TMP_Text sectionPrefab;
    [SerializeField] private TMP_Text descTitle;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private Button backButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private ControlsConfig controlsConfig;

    private const float RevertSeconds = 10f;

    private readonly List<SettingRowView> _rows = new List<SettingRowView>();
    private readonly Dictionary<ControlsConfig.Row, SettingRowView> _controlRows = new Dictionary<ControlsConfig.Row, SettingRowView>();
    private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();
    private PlayerConfig _config;
    private SettingRowView _focusedRow;
    private Tab _tab;
    private bool _showAccount;

    // display changes wait for Apply, then for a confirmation
    private int _pendingMode;
    private Vector2Int _pendingResolution;
    private bool _displayDirty;
    private bool _reverting;
    private float _revertAt;
    private int _previousMode;
    private Vector2Int _previousResolution;

    private void Awake()
    {
        // one settings screen for the whole game: the first scene that has one keeps it, copies in later scenes remove themselves
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabButtons[i].onClick.AddListener(() => ShowTab((Tab)index));
        }
        backButton.onClick.AddListener(Close);
        resetButton.onClick.AddListener(ResetCurrentTab);
        applyButton.onClick.AddListener(OnApplyPressed);

        screenRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Open(bool showAccount)
    {
        _config = GameSettings.Config;
        if (_config == null)
        {
            Debug.LogError("[SettingsScreen] PlayerConfig is not available yet.");
            return;
        }

        _showAccount = showAccount;
        _displayDirty = false;
        _reverting = false;
        toastText.gameObject.SetActive(false);

        screenRoot.SetActive(true);
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        ShowTab(Tab.General);
    }

    public void Close()
    {
        if (!IsOpen) return;

        ControlsRebinder.CancelRebind();
        if (_reverting) RevertDisplay(false);

        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        screenRoot.SetActive(false);
        GameSettings.SaveNow();
        Closed?.Invoke();
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (IsOpen) ShowTab(_tab);
    }

    private void Update()
    {
        if (!IsOpen) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        UpdateRevertPrompt();

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            EscapeConsumedFrame = Time.frameCount;
            bool handledByRebind = ControlsRebinder.IsRebinding || ControlsRebinder.LastEndFrame == Time.frameCount;
            if (handledByRebind) return;

            if (_reverting) RevertDisplay(true);
            else Close();
            return;
        }

        if (ControlsRebinder.IsRebinding) return;

        if (keyboard.qKey.wasPressedThisFrame) ShowTab((Tab)(((int)_tab + 3) % 4));
        else if (keyboard.eKey.wasPressedThisFrame) ShowTab((Tab)(((int)_tab + 1) % 4));
        else if (keyboard.rKey.wasPressedThisFrame) ResetCurrentTab();
        else if (keyboard.enterKey.wasPressedThisFrame && (_focusedRow == null || !_focusedRow.HandlesSubmit)) OnApplyPressed();
    }

    private void ShowTab(Tab tab)
    {
        ControlsRebinder.CancelRebind();
        _tab = tab;
        _focusedRow = null;

        for (int i = 0; i < tabUnderlines.Length; i++) tabUnderlines[i].SetActive(i == (int)tab);
        applyButton.gameObject.SetActive(tab == Tab.Video);

        ClearRows();
        switch (tab)
        {
            case Tab.General: BuildGeneral(); break;
            case Tab.Video: BuildVideo(); break;
            case Tab.Audio: BuildAudio(); break;
            case Tab.Controls: BuildControls(); break;
        }

        content.anchoredPosition = Vector2.zero;
        if (_rows.Count > 0) StartCoroutine(SelectNextFrame(_rows[0]));
    }

    private IEnumerator SelectNextFrame(SettingRowView row)
    {
        yield return null;
        if (row != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(row.gameObject);
    }

    private void ClearRows()
    {
        foreach (Transform child in content) Destroy(child.gameObject);
        _rows.Clear();
        _controlRows.Clear();
    }

    private void ResetCurrentTab()
    {
        switch (_tab)
        {
            case Tab.General: GameSettings.ResetGeneral(); break;
            case Tab.Video:
                GameSettings.ResetVideo();
                _displayDirty = false;
                break;
            case Tab.Audio: GameSettings.ResetAudio(); break;
            case Tab.Controls: ControlsRebinder.ResetActions(controlsConfig); break;
        }
        ShowTab(_tab);
    }

    private static string T(string key, params object[] args) => InventoryText.Get(key, args);

    private void AddSection(string key)
    {
        var header = Instantiate(sectionPrefab, content);
        header.text = T(key);
    }

    private SettingRowView AddRow()
    {
        var row = Instantiate(rowPrefab, content);
        row.Focused += OnRowFocused;
        _rows.Add(row);
        return row;
    }

    private void AddToggle(string key, bool value, Action<bool> onChanged) =>
        AddRow().SetupToggle(T(key), T(key + "_DESC"), value, onChanged);

    private void AddSlider(string key, float min, float max, float value, Func<float, string> format, Action<float> onChanged) =>
        AddRow().SetupSlider(T(key), T(key + "_DESC"), min, max, value, true, format, onChanged);

    private SettingRowView AddSelector(string key, IList<string> options, int index, Action<int> onChanged)
    {
        var row = AddRow();
        row.SetupSelector(T(key), T(key + "_DESC"), options, index, onChanged);
        return row;
    }

    private void OnRowFocused(SettingRowView row)
    {
        _focusedRow = row;
        descTitle.text = row.Title.ToUpperInvariant();
        descText.text = row.Description;
        EnsureVisible((RectTransform)row.transform);
    }

    // keyboard focus on a row that is half outside the list scrolls the list
    private void EnsureVisible(RectTransform row)
    {
        var rowCorners = new Vector3[4];
        var viewCorners = new Vector3[4];
        row.GetWorldCorners(rowCorners);
        scroll.viewport.GetWorldCorners(viewCorners);

        float scale = Mathf.Max(0.0001f, content.lossyScale.y);
        var position = content.anchoredPosition;
        if (rowCorners[1].y > viewCorners[1].y) position.y -= (rowCorners[1].y - viewCorners[1].y) / scale;
        else if (rowCorners[0].y < viewCorners[0].y) position.y += (viewCorners[0].y - rowCorners[0].y) / scale;

        position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, content.rect.height - scroll.viewport.rect.height));
        content.anchoredPosition = position;
    }

    private void BuildGeneral()
    {
        AddSection("SET_SEC_LANGUAGE");
        BuildLanguageRow();
        AddToggle("SET_SHOW_FPS", _config.showFps, v => { _config.showFps = v; GameSettings.MarkChanged(); });
        AddToggle("SET_DAMAGE_NUMBERS", _config.showDamageText, v => { _config.showDamageText = v; GameSettings.MarkChanged(); });
        AddSlider("SET_SCREEN_SHAKE", 0f, 100f, _config.activeCameraShake ? _config.cameraShakeStrength * 100f : 0f,
            v => $"{v:0}%", v =>
            {
                _config.activeCameraShake = v > 0f;
                _config.cameraShakeStrength = v / 100f;
                GameSettings.MarkChanged();
            });

        AddSection("SET_SEC_INTERFACE");
        AddSlider("SET_UI_SCALE", 75f, 125f, _config.uiScale, v => $"{v:0}%", v => { _config.uiScale = (int)v; GameSettings.MarkChanged(); });
        AddToggle("SET_TUTORIAL_HINTS", _config.tutorialHints, v => { _config.tutorialHints = v; GameSettings.MarkChanged(); });

        if (!_showAccount) return;

        AddSection("SET_SEC_ACCOUNT");
        AddRow().SetupButton(T("SET_PLAYER_NAME"), T("SET_PLAYER_NAME_DESC"), T("SET_CHANGE"), () =>
        {
            Close();
            ChangeNameRequested?.Invoke();
        });
        AddRow().SetupButton(T("SET_SIGN_OUT"), T("SET_SIGN_OUT_DESC"), T("SET_SIGN_OUT_BUTTON"), () =>
        {
            Close();
            SignOutRequested?.Invoke();
        });
    }

    private void BuildLanguageRow()
    {
        if (!LocalizationSettings.InitializationOperation.IsDone) LocalizationSettings.InitializationOperation.WaitForCompletion();

        var locales = LocalizationSettings.AvailableLocales.Locales;
        var names = new List<string>();
        foreach (var locale in locales)
        {
            var culture = locale.Identifier.CultureInfo;
            string native = culture != null ? culture.NativeName : locale.LocaleName;
            names.Add(string.IsNullOrEmpty(native) ? locale.LocaleName : char.ToUpperInvariant(native[0]) + native.Substring(1));
        }

        int index = Mathf.Max(0, locales.IndexOf(LocalizationSettings.SelectedLocale));
        AddSelector("SET_LANGUAGE", names, index, i =>
        {
            _config.language = locales[i].Identifier.Code;
            GameSettings.MarkChanged();
        });
    }

    private void BuildVideo()
    {
        BuildResolutionList();
        if (!_displayDirty)
        {
            _pendingMode = Mathf.Clamp(_config.displayMode, 0, 2);
            _pendingResolution = ConfigResolution();
        }

        AddSection("SET_SEC_DISPLAY");
        AddSelector("SET_DISPLAY_MODE", new[] { T("SET_FULLSCREEN"), T("SET_BORDERLESS"), T("SET_WINDOWED") }, _pendingMode, i =>
        {
            _pendingMode = i;
            _displayDirty = true;
        });

        var labels = new List<string>();
        foreach (var resolution in _resolutions) labels.Add($"{resolution.x} x {resolution.y}");
        AddSelector("SET_RESOLUTION", labels, Mathf.Max(0, _resolutions.IndexOf(_pendingResolution)), i =>
        {
            _pendingResolution = _resolutions[i];
            _displayDirty = true;
        });

        AddToggle("SET_VSYNC", _config.vsync, v => { _config.vsync = v; GameSettings.MarkChanged(); });
        AddSelector("SET_FPS_LIMIT", new[] { "30", "60", "120", T("SET_UNLIMITED") },
            _config.fps == 30 ? 0 : _config.fps == 60 ? 1 : _config.fps == 120 ? 2 : 3, i =>
            {
                _config.fps = i == 0 ? 30 : i == 1 ? 60 : i == 2 ? 120 : -1;
                GameSettings.MarkChanged();
            });

        AddSection("SET_SEC_QUALITY");
        AddSelector("SET_QUALITY", QualitySettings.names, _config.quality, i => { _config.quality = i; GameSettings.MarkChanged(); });
        AddSelector("SET_SHADOWS", new[] { T("SET_SHADOW_OFF"), T("SET_SHADOW_LOW"), T("SET_SHADOW_MEDIUM"), T("SET_SHADOW_HIGH") },
            _config.shadowQuality, i => { _config.shadowQuality = i; GameSettings.MarkChanged(); });
        AddToggle("SET_POST_PROCESSING", _config.postProcessing, v => { _config.postProcessing = v; GameSettings.MarkChanged(); });
        AddSlider("SET_BRIGHTNESS", 0f, 100f, _config.brightness, v => $"{v:0}", v => { _config.brightness = (int)v; GameSettings.MarkChanged(); });
    }

    private Vector2Int ConfigResolution() =>
        new Vector2Int(_config.resolutionWidth > 0 ? _config.resolutionWidth : Screen.width,
                       _config.resolutionHeight > 0 ? _config.resolutionHeight : Screen.height);

    private void BuildResolutionList()
    {
        _resolutions.Clear();
        var seen = new HashSet<Vector2Int>();
        foreach (var resolution in Screen.resolutions)
        {
            var size = new Vector2Int(resolution.width, resolution.height);
            if (size.x >= 1024 && seen.Add(size)) _resolutions.Add(size);
        }

        var current = ConfigResolution();
        if (seen.Add(current)) _resolutions.Add(current);
        if (_displayDirty && seen.Add(_pendingResolution)) _resolutions.Add(_pendingResolution);

        _resolutions.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
    }

    private void OnApplyPressed()
    {
        if (_tab != Tab.Video) return;

        if (_reverting) KeepDisplay();
        else ApplyDisplay();
    }

    private void ApplyDisplay()
    {
        if (!_displayDirty) return;

        _previousMode = _config.displayMode;
        _previousResolution = ConfigResolution();

        _config.displayMode = _pendingMode;
        _config.resolutionWidth = _pendingResolution.x;
        _config.resolutionHeight = _pendingResolution.y;
        SettingsApplier.ApplyDisplay(_config);

        _displayDirty = false;
        _reverting = true;
        _revertAt = Time.unscaledTime + RevertSeconds;
    }

    private void KeepDisplay()
    {
        _reverting = false;
        toastText.gameObject.SetActive(false);
        GameSettings.MarkChanged();
    }

    private void RevertDisplay(bool rebuild)
    {
        _reverting = false;
        toastText.gameObject.SetActive(false);

        _config.displayMode = _previousMode;
        _config.resolutionWidth = _previousResolution.x;
        _config.resolutionHeight = _previousResolution.y;
        SettingsApplier.ApplyDisplay(_config);

        if (rebuild && IsOpen) ShowTab(_tab);
    }

    private void UpdateRevertPrompt()
    {
        if (!_reverting) return;

        float remaining = _revertAt - Time.unscaledTime;
        if (remaining <= 0f)
        {
            RevertDisplay(true);
            return;
        }

        toastText.gameObject.SetActive(true);
        toastText.text = T("SET_KEEP_PROMPT", Mathf.CeilToInt(remaining));
    }

    private void BuildAudio()
    {
        AddSection("SET_SEC_VOLUME");
        AddVolume("SET_MASTER", AudioChannel.Master, _config.masterVolume);
        AddVolume("SET_MUSIC", AudioChannel.Music, _config.musicVolume);
        AddVolume("SET_SFX", AudioChannel.Sfx, _config.sfxVolume);

        AddSection("SET_SEC_OPTIONS");
        AddToggle("SET_MUTE_UNFOCUSED", _config.muteWhenUnfocused, v => { _config.muteWhenUnfocused = v; GameSettings.MarkChanged(); });
    }

    private void AddVolume(string key, AudioChannel channel, float fallback)
    {
        float current = AudioService.Instance != null ? AudioService.Instance.GetVolume(channel) : fallback;
        AddSlider(key, 0f, 100f, current * 100f, v => $"{v:0}%", v =>
        {
            if (AudioService.Instance != null) AudioService.Instance.SetVolume(channel, v / 100f); // applies, updates PlayerConfig and saves
        });
    }

    private void BuildControls()
    {
        if (controlsConfig == null)
        {
            Debug.LogError("[SettingsScreen] ControlsConfig is not assigned.");
            return;
        }

        foreach (ControlSection section in Enum.GetValues(typeof(ControlSection)))
        {
            AddSection(section == ControlSection.Movement ? "SET_SEC_MOVEMENT" : "SET_SEC_COMBAT");

            foreach (var row in controlsConfig.rows)
            {
                if (row.section != section) continue;

                var view = AddRow();
                var captured = row;
                view.SetupKey(T(row.labelKey), T("SET_CONTROL_DESC"), ControlsRebinder.DisplayName(row.actionPath), () => BeginRebind(captured, view));
                _controlRows[row] = view;
            }
        }
    }

    private void BeginRebind(ControlsConfig.Row row, SettingRowView view)
    {
        if (ControlsRebinder.IsRebinding) return;

        view.SetKeyText(T("SET_LISTENING"), true);
        ControlsRebinder.StartRebind(controlsConfig, row.actionPath, success => RefreshControlTexts());
    }

    private void RefreshControlTexts()
    {
        foreach (var pair in _controlRows)
        {
            if (pair.Value != null) pair.Value.SetKeyText(ControlsRebinder.DisplayName(pair.Key.actionPath), false);
        }
    }
}