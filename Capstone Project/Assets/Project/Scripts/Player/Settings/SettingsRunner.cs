using System.Collections; 
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;

// Hidden object that lives for the whole game: applies the saved settings once the config is loaded, re-applies the scene-bound ones
// after every scene load, saves a moment after the last change and handles window focus.
public class SettingsRunner : MonoBehaviour
{
    private static SettingsRunner _instance;
    private float _saveAt = -1f;
    private bool _applied;

    public static SettingsRunner Instance
    {
        get
        {
            if (_instance != null) return _instance;

            var go = new GameObject("SettingsRunner") { hideFlags = HideFlags.HideAndDontSave };
            _instance = go.AddComponent<SettingsRunner>();
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() => _ = Instance;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.focusChanged += OnFocusChanged;
        Application.quitting += OnQuitting;
    }

    private IEnumerator Start()
    {
        while (GameSettings.Config == null) yield return new WaitForSecondsRealtime(0.25f);
        yield return LocalizationSettings.InitializationOperation;

        SettingsApplier.ApplyDisplay(GameSettings.Config);
        SettingsApplier.ApplyAll();
        _applied = true;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.focusChanged -= OnFocusChanged;
        Application.quitting -= OnQuitting;
    }

    public void ScheduleSave() => _saveAt = Time.unscaledTime + 0.75f;

    private void Update()
    {
        if (_saveAt > 0f && Time.unscaledTime >= _saveAt) Flush();
    }

    private void Flush()
    {
        _saveAt = -1f;
        GameSettings.SaveNow();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_applied) SettingsApplier.ApplyAll();
    }

    private void OnFocusChanged(bool focused) => SettingsApplier.ApplyFocusMute(focused);

    private void OnQuitting()
    {
        if (_saveAt > 0f) Flush();
        SettingsApplier.RestoreOnQuit();
    }
}