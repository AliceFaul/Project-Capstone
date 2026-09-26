using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using UnityEngine.Localization;
using Random = UnityEngine.Random;

public class MainMenu : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineCamera overviewCamera;
    [SerializeField] private CinemachineCamera playerFocusCamera;
    [SerializeField] private CinemachineCamera changeSkinCamera;
    
    [Header("UI")]
    [SerializeField] private MainMenuScreen screen;

    [Header("Popup")]
    [SerializeField] private LocalizedString quitConfirmContent;
    [SerializeField] private LocalizedString nameEntryPromptContent;

    private IPopupService _popupService;
    private PlayerDataConfig _config;
    
    private const int InactivePriority = 0;
    private const int ActivePriority = 20;

    private void Start()
    {
        _popupService = UIManager.Instance?.GetPopupService();
        var configMg = StartupProcessor.Instance?.GetService<ConfigManager>();
        if (configMg != null && configMg.GetConfig(out PlayerDataConfig config)) _config = config;
        CheckFirstTimePlayerName();
    }

    private void OnEnable()
    {
        if(screen == null) return;
        
        screen.StartGameClicked += StartGame;
        screen.OptionClicked += Option;
        screen.QuitClicked += QuitGame;
        screen.ChangeCosmeticClicked += HandleChangeCosmetic;
        screen.CloseCosmeticClicked += HandleCloseCosmetic;
        screen.PlayerNameClicked += OpenNameEntry;
    }

    private void OnDisable()
    {
        if(screen == null) return;
        
        screen.StartGameClicked -= StartGame;
        screen.OptionClicked -= Option;
        screen.QuitClicked -= QuitGame;
        screen.ChangeCosmeticClicked -= HandleChangeCosmetic;
        screen.CloseCosmeticClicked -= HandleCloseCosmetic;
        screen.PlayerNameClicked -= OpenNameEntry;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)) HandleEscapeKey();
    }

    public void OpenMainMenu()
    {
        StartCoroutine(FocusToPlayer());
    }
    
    private IEnumerator FocusToPlayer()
    {
        SetPriority(playerFocusCamera, ActivePriority);
        SetPriority(overviewCamera, InactivePriority);
        yield return WaitForBlend();
        screen.Show();
    }

    private void HandleChangeCosmetic()
    {
        SetPriority(changeSkinCamera, ActivePriority);
        SetPriority(playerFocusCamera, InactivePriority);
        screen.ShowChangeCosmeticPanel();
    }

    private void HandleCloseCosmetic()
    {
        SetPriority(playerFocusCamera, ActivePriority);
        SetPriority(changeSkinCamera, InactivePriority);
        screen.HideChangeCosmeticPanel();
    }
    
    private void StartGame()
    {
        _ = SceneLoader.Instance.LoadScene("Mhieu", false);
    }
    
    private void Option()
    {
        Debug.Log($"[MainMenu] Open Options!");
        screen.ShowSettingsPanel();
    }

    private void QuitGame()
    {
        Debug.Log("[MainMenu] Quit Game!");

        if (_popupService != null)
        {
            _popupService.Create(
                prefabId: "QuitConfirmPopup",
                instanceId: $"quit_confirm_popup_{Time.time}_{Random.Range(0, 9999)}",
                content: quitConfirmContent,
                onClick1: () =>
                {
                    ConfirmQuit();
                    return true;
                });
        }
        else
        {
            ConfirmQuit();
        }
    }

    private void ConfirmQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;        
#endif
    }

    private void HandleEscapeKey()
    {
        if(screen.IsSettingsPanelOpen) screen.HideSettingsPanel();
        else if(screen.IsCosmeticPanelOpen) HandleCloseCosmetic();
        else QuitGame();
    }

    private void CheckFirstTimePlayerName()
    {
        if (_config != null && (string.IsNullOrEmpty(_config.DisplayName) || _config.DisplayName == "Unknown"))
        {
            OpenNameEntry();
        }
    }

    private void OpenNameEntry()
    {
        if(_popupService == null || _config == null) return;
        PlayerNameHandler component = null;
        
        GameObject popup = _popupService.Create(
            prefabId: "NameEntryPopup",
            instanceId: $"name_entry_popup_{Time.time}_{Random.Range(0, 9999)}",
            content: nameEntryPromptContent,
            onClick1: () => component != null && component.OnConfirm(),
            onClick2: null);

        if (popup != null && popup.TryGetComponent<PlayerNameHandler>(out component))
        {
            component.Initialize(_config);
        }
    }

    private WaitUntil WaitForBlend()
    {
        return new WaitUntil(() => brain == null || !brain.IsBlending);
    }

    private void SetPriority(CinemachineCamera cam, int priority)
    {
        if(cam == null) return;
        cam.Priority = new PrioritySettings { Enabled = true, Value = priority };
    }
}