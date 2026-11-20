using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GameSettings
{
    public static event Action Changed;
    public static event Action BindingsChanged;

    private static PlayerConfig _config;

    public static PlayerConfig Config
    {
        get
        {
            if (_config != null) return _config;

            try
            {
                var startup = StartupProcessor.Instance;
                if (startup != null && startup.GetService(out ConfigManager manager) && manager.GetConfig(out PlayerConfig config))
                    _config = config;
            }
            catch (NullReferenceException)
            {
                // the startup pipeline has not created its service registry yet
            }
            return _config;
        }
    }

    public static bool ShowFps => Config != null && Config.showFps;
    public static bool ShowDamageText => Config == null || Config.showDamageText;
    public static bool CameraShakeEnabled => Config == null || Config.activeCameraShake;
    public static float CameraShakeStrength => Config == null ? 1f : (Config.activeCameraShake ? Config.cameraShakeStrength : 0f);
    public static bool TutorialHints => Config == null || Config.tutorialHints;

    public static void MarkChanged(bool save = true)
    {
        SettingsApplier.ApplyAll();
        Changed?.Invoke();
        if (save) SaveSoon();
    }

    public static void SaveSoon() => SettingsRunner.Instance.ScheduleSave();

    public static void SaveNow()
    {
        var config = Config;
        if (config == null) return;

        new JsonWriter(Application.persistentDataPath).Write(config, config.name + ".json"); // same call AudioService uses
    }

    // ---- key bindings (rebinds are stored as the Input System's override JSON)
    public static void ApplyBindings(InputActionAsset asset)
    {
        var json = Config != null ? Config.bindingOverrides : null;
        if (asset != null && !string.IsNullOrEmpty(json)) asset.LoadBindingOverridesFromJson(json);
    }

    public static void SaveBindings(string overridesJson)
    {
        if (Config == null) return;

        Config.bindingOverrides = overridesJson;
        SaveSoon();
        BindingsChanged?.Invoke();
    }

    // ---- "Reset section"
    public static void ResetGeneral()
    {
        var c = Config;
        if (c == null) return;

        c.showFps = false;
        c.showDamageText = true;
        c.activeCameraShake = true;
        c.cameraShakeStrength = 1f;
        c.uiScale = 100;
        c.tutorialHints = true;
        MarkChanged();
    }

    public static void ResetVideo()
    {
        var c = Config;
        if (c == null) return;

        c.displayMode = 0;
        c.resolutionWidth = 0;
        c.resolutionHeight = 0;
        c.vsync = false;
        c.fps = 60;
        c.quality = Mathf.Max(0, QualitySettings.names.Length - 1);
        c.shadowQuality = 3;
        c.postProcessing = true;
        c.brightness = 50;
        MarkChanged();
        SettingsApplier.ApplyDisplay(c);
    }

    public static void ResetAudio()
    {
        var c = Config;
        if (c == null) return;

        if (AudioService.Instance != null)
        {
            AudioService.Instance.SetVolume(AudioChannel.Master, 1f);
            AudioService.Instance.SetVolume(AudioChannel.Music, 0.8f);
            AudioService.Instance.SetVolume(AudioChannel.Sfx, 1f);
        }
        c.muteWhenUnfocused = true;
        MarkChanged();
    }
}