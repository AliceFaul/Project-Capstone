using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Screen")]
    [SerializeField] private GameObject screenRoot;
    [SerializeField] private GameObject runPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject confirmRoot;
    [SerializeField] private UIBlurBackdrop blur;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button routeMapButton; // optional (run only)
    [SerializeField] private Button abandonButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button keepPlayingButton;
    [SerializeField] private Button confirmAbandonButton;

    [Header("Run panel")]
    [SerializeField] private TMP_Text runLevelText;
    [SerializeField] private TMP_Text runDifficultyText;
    [SerializeField] private TMP_Text runNodeText;
    [SerializeField] private TMP_Text runTimeText;
    [SerializeField] private TMP_Text runKillsText;
    [SerializeField] private TMP_Text runGoldText;
    [SerializeField] private TMP_Text runPowerText;

    [Header("Lobby panel")]
    [SerializeField] private PlayerModelPreview modelPreview;
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text lobbyLevelText;
    [SerializeField] private TMP_Text lobbyGoldText;
    [SerializeField] private TMP_Text lobbyGemText;
    [SerializeField] private TMP_Text lobbyPowerText;
    [SerializeField] private TMP_Text lobbyHpText;
    [SerializeField] private TMP_Text lobbyArmorText;
    [SerializeField] private TMP_Text lobbyMoveText;
    [SerializeField] private TMP_Text lobbyCritChanceText;
    [SerializeField] private TMP_Text lobbyCritDamageText;
    [SerializeField] private TMP_Text lobbyClearedText;

    [Header("Data")]
    [SerializeField] private LevelCatalog catalog;
    [SerializeField] private CombatPowerConfig powerConfig;

    [Header("Scenes (names in Build Settings)")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private InventoryScreenController _inventory;
    private LevelSelectView _levelSelect;
    private ShopSceneDirector[] _shops;
    private LevelRunController _run;
    private bool _open;
    private bool _opening;
    private bool _runMode;
    private bool _blockedLastFrame;
    private float _previousTimeScale = 1f;

    public bool IsOpen => _open;

    private void Start()
    {
        screenRoot.SetActive(false);
        confirmRoot.SetActive(false);

        resumeButton.onClick.AddListener(Close);
        settingsButton.onClick.AddListener(OpenSettings);
        if (routeMapButton != null) routeMapButton.onClick.AddListener(OpenRouteMap);
        abandonButton.onClick.AddListener(ShowConfirm);
        quitButton.onClick.AddListener(QuitToMainMenu);
        keepPlayingButton.onClick.AddListener(HideConfirm);
        confirmAbandonButton.onClick.AddListener(Abandon);

        _inventory = FindFirstObjectByType<InventoryScreenController>(FindObjectsInactive.Include);
        _levelSelect = FindFirstObjectByType<LevelSelectView>(FindObjectsInactive.Include);
        _shops = FindObjectsByType<ShopSceneDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        _run = LevelRunController.Instance;
        if (_run != null)
        {
            _run.RunStarted += OnRunStarted;
            if (_run.IsRunning) RunStats.Begin(GetData());
        }
        SettingsScreen.Closed += OnSettingsClosed;
        NodeMapView.Closed += OnRouteMapClosed;
    }

    private void OnDestroy()
    {
        if (_run != null) _run.RunStarted -= OnRunStarted;
        SettingsScreen.Closed -= OnSettingsClosed;
        NodeMapView.Closed -= OnRouteMapClosed;
        RunStats.Stop();

        if (_open) Time.timeScale = _previousTimeScale <= 0.01f ? 1f : _previousTimeScale;
    }

    private void OnRunStarted(NodeGraph graph) => RunStats.Begin(GetData());

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        // the settings screen uses Esc itself (also in this frame)
        if (SettingsScreen.IsOpen || SettingsScreen.EscapeConsumedFrame == Time.frameCount) return;
        if (NodeMapView.IsOpen || NodeMapView.EscapeConsumedFrame == Time.frameCount) return;

        if (_open)
        {
            if (_opening) return;
            if (confirmRoot.activeSelf) HideConfirm();
            else Close();
        }
        else if (CanOpen())
        {
            Open();
        }
    }

    // Other screens close themselves on Esc, so "was something open at the end of last frame" tells us the Esc was theirs.
    private void LateUpdate() => _blockedLastFrame = AnyOtherScreenOpen();

    private bool AnyOtherScreenOpen()
    {
        if (_inventory != null && _inventory.IsOpen) return true;
        if (_levelSelect != null && _levelSelect.IsOpen) return true;
        if (SettingsScreen.IsOpen || NodeMapView.IsOpen) return true;

        if (_shops != null)
        {
            foreach (var shop in _shops)
            {
                if (shop != null && shop.IsOpen) return true;
            }
        }
        return false;
    }

    private bool CanOpen()
    {
        if (RunSession.Instance != null && RunSession.Instance.State != RunState.Playing) return false;
        if (_blockedLastFrame || Time.timeScale < 0.01f) return false;

        var run = LevelRunController.Instance;
        return run == null || !run.IsTransitioning;
    }

    public void Open()
    {
        if (_open) return;

        var run = LevelRunController.Instance;
        _runMode = run != null && run.IsRunning;
        _open = true;

        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        LockInput(true);
        AudioService.Instance?.SetOverlay(this, true);

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        _opening = true;
        screenRoot.SetActive(false); // so the screenshot for the blur does not contain the menu
        confirmRoot.SetActive(false);
        if (blur != null) yield return blur.Capture();

        Refresh();
        screenRoot.SetActive(true);
        if (!_runMode && modelPreview != null) modelPreview.Show();

        Select(resumeButton);
        _opening = false;
    }

    public void Close()
    {
        if (!_open) return;

        _open = false;
        _opening = false;
        StopAllCoroutines();

        screenRoot.SetActive(false);
        confirmRoot.SetActive(false);
        if (modelPreview != null) modelPreview.Hide();
        if (blur != null) blur.Release();

        Time.timeScale = _previousTimeScale <= 0.01f ? 1f : _previousTimeScale;
        LockInput(false);
        AudioService.Instance?.SetOverlay(this, false);
    }

    private void LockInput(bool locked)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        player.CmdCombatLocked(locked);
        var artifacts = player.ArtifactController;
        if (artifacts != null) artifacts.InputLocked = locked;
    }

    private static void Select(Button button)
    {
        if (EventSystem.current != null && button != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void OpenSettings()
    {
        if (SettingsScreen.Instance == null)
        {
            Debug.LogError("[PauseMenu] No SettingsScreen in the scene: place the SettingsCanvas prefab here too (extra copies remove themselves).");
            return;
        }
        SettingsScreen.Instance.Open(false);
    }

    private void OnSettingsClosed()
    {
        if (_open && !_opening) Select(settingsButton);
    }

    private void OpenRouteMap()
    {
        if (NodeMapView.Instance == null)
        {
            Debug.LogError("[PauseMenu] No NodeMapView in the scene: build the route map UI (Tools > Map > Build Node Map UI).");
            return;
        }
        NodeMapView.Instance.Open();
    }

    private void OnRouteMapClosed()
    {
        if (_open && !_opening && routeMapButton != null) Select(routeMapButton);
    }

    private void ShowConfirm()
    {
        confirmRoot.SetActive(true);
        Select(keepPlayingButton);
    }

    private void HideConfirm()
    {
        confirmRoot.SetActive(false);
        Select(abandonButton);
    }

    // Leaves the run on purpose: not a failure (no summary), nothing recorded; items and gold are kept and saved.
    private void Abandon()
    {
        var run = LevelRunController.Instance;
        Close();

        if (run != null) run.Abandon();
        RunStats.Stop();
        RunSettings.Clear();
        SaveHelper.SaveNow();
        LoadScene(lobbySceneName);
    }

    private void QuitToMainMenu()
    {
        var run = LevelRunController.Instance;
        bool inRun = _runMode;
        Close();

        if (inRun && run != null) run.Abandon();
        RunStats.Stop();
        RunSettings.Clear();
        SaveHelper.SaveNow();
        LoadScene(mainMenuSceneName);
    }

    private static async void LoadScene(string sceneName)
    {
        try
        {
            if (SceneLoader.Instance == null)
            {
                Debug.LogError("[PauseMenu] SceneLoader.Instance is missing.");
                return;
            }
            await SceneLoader.Instance.LoadScene(sceneName, false);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private static string T(string key, params object[] args) => InventoryText.Get(key, args);

    private static PlayerDataConfig GetData()
    {
        var manager = StartupProcessor.Instance != null ? StartupProcessor.Instance.GetService<ConfigManager>() : null;
        return manager != null && manager.GetConfig(out PlayerDataConfig data) ? data : null;
    }

    private void Refresh()
    {
        var data = GetData();
        runPanel.SetActive(_runMode);
        lobbyPanel.SetActive(!_runMode);
        abandonButton.gameObject.SetActive(_runMode);
        if (routeMapButton != null) routeMapButton.gameObject.SetActive(_runMode);

        if (_runMode) RefreshRun(data);
        else RefreshLobby(data);
    }

    private void RefreshRun(PlayerDataConfig data)
    {
        var run = LevelRunController.Instance;
        var level = run != null ? run.Level : null;
        var settings = level != null ? level.GetDifficulty(run.DifficultyIndex) : null;

        string levelName = level != null ? level.GetName() : "-";
        string difficulty = settings != null ? T("DIFFICULTY_" + settings.difficulty.ToString().ToUpper()) : "-";
        subtitleText.text = $"{levelName} - {difficulty}".ToUpperInvariant();

        runLevelText.text = levelName;
        runDifficultyText.text = difficulty;

        var node = run != null ? run.Current : null;
        runNodeText.text = node != null && run.Graph != null
            ? $"{node.Layer + 1} / {run.Graph.LayerCount} - {T("NODE_" + node.Type.ToString().ToUpper())}"
            : "-";

        runTimeText.text = FormatTime(RunStats.ElapsedSeconds);
        runKillsText.text = RunStats.EnemiesDefeated.ToString("N0");
        runGoldText.text = RunStats.GoldEarned.ToString("N0");
        runPowerText.text = CombatPower.Calculate(data, powerConfig).ToString("N0");
    }

    private void RefreshLobby(PlayerDataConfig data)
    {
        subtitleText.text = T("PAUSE_LOBBY");

        if (data != null)
        {
            lobbyNameText.text = string.IsNullOrEmpty(data.DisplayName) ? "-" : data.DisplayName;
            lobbyLevelText.text = T("PAUSE_LEVEL_FMT", data.Level);
            lobbyGoldText.text = data.Currency != null ? data.Currency.Gold().ToString("N0") : "-";
            lobbyGemText.text = data.Currency != null ? data.Currency.Gem().ToString("N0") : "-";
        }
        lobbyPowerText.text = CombatPower.Calculate(data, powerConfig).ToString("N0");

        var player = FindFirstObjectByType<PlayerRuntime>();
        lobbyHpText.text = player != null ? player.TotalHealth.ToString() : "-";
        lobbyArmorText.text = player != null ? player.TotalArmor.ToString() : "-";
        lobbyMoveText.text = player != null ? player.TotalSpeed.ToString("0.0") : "-";
        lobbyCritChanceText.text = player != null ? $"{player.TotalCritChance:0.#}%" : "-";
        lobbyCritDamageText.text = player != null ? $"+{player.TotalCritDamage:0.#}%" : "-";

        int cleared = 0;
        int total = 0;
        if (catalog != null)
        {
            foreach (var level in catalog.Levels)
            {
                if (level == null) continue;
                total++;
                var progress = data != null ? data.GetLevelProgress(level.id) : null;
                if (progress is { timesCompleted: > 0 }) cleared++;
            }
        }
        lobbyClearedText.text = $"{cleared} / {total}";
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return total >= 3600 ? $"{total / 3600}:{total / 60 % 60:00}:{total % 60:00}" : $"{total / 60:00}:{total % 60:00}";
    }
}