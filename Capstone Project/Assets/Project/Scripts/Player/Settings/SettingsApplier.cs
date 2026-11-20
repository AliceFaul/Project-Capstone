using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Puts PlayerConfig values into effect. Display mode / resolution are applied separately (ApplyDisplay) because changing them needs a confirmation.
public static class SettingsApplier
{
    private static float _originalShadowDistance = -1f;
    private static Volume _brightnessVolume;
    private static ColorAdjustments _colorAdjustments;
    private static bool _focused = true;
    private static readonly Dictionary<int, (Vector2 reference, float scaleFactor)> UiBase = new Dictionary<int, (Vector2, float)>();

    public static void ApplyAll()
    {
        var c = GameSettings.Config;
        if (c == null) return;

        ApplyLanguage(c);
        ApplyQuality(c);
        ApplyShadows(c);
        ApplyPostProcessing(c);
        ApplyBrightness(c);
        ApplyUiScale(c);
        ApplyFocusMute(_focused);
    }

    public static void ApplyDisplay(PlayerConfig c)
    {
        var mode = c.displayMode == 1 ? FullScreenMode.FullScreenWindow
            : c.displayMode == 2 ? FullScreenMode.Windowed
            : FullScreenMode.ExclusiveFullScreen;

        int width = c.resolutionWidth > 0 ? c.resolutionWidth : Screen.currentResolution.width;
        int height = c.resolutionHeight > 0 ? c.resolutionHeight : Screen.currentResolution.height;
        Screen.SetResolution(width, height, mode);
    }

    private static void ApplyLanguage(PlayerConfig c)
    {
        if (string.IsNullOrEmpty(c.language)) return;

        var locale = LocalizationSettings.AvailableLocales != null ? LocalizationSettings.AvailableLocales.GetLocale(c.language) : null;
        if (locale != null && LocalizationSettings.SelectedLocale != locale) LocalizationSettings.SelectedLocale = locale;
    }

    private static void ApplyQuality(PlayerConfig c)
    {
        int level = Mathf.Clamp(c.quality, 0, QualitySettings.names.Length - 1);
        if (QualitySettings.GetQualityLevel() != level) QualitySettings.SetQualityLevel(level, true);

        QualitySettings.vSyncCount = c.vsync ? 1 : 0;
        Application.targetFrameRate = c.vsync ? -1 : (c.fps > 0 ? c.fps : -1); // -1 = unlimited
    }

    // Shadow quality = how far shadows are drawn (the URP asset's shadow distance).
    private static void ApplyShadows(PlayerConfig c)
    {
        var asset = UniversalRenderPipeline.asset;
        if (asset == null) return;

        if (_originalShadowDistance < 0f) _originalShadowDistance = asset.shadowDistance;
        float factor = c.shadowQuality == 0 ? 0f : c.shadowQuality == 1 ? 0.35f : c.shadowQuality == 2 ? 0.7f : 1f;
        asset.shadowDistance = _originalShadowDistance * factor;
    }

    // The URP asset is a project file: put its value back when the game (or Play mode) ends.
    public static void RestoreOnQuit()
    {
        var asset = UniversalRenderPipeline.asset;
        if (asset != null && _originalShadowDistance >= 0f) asset.shadowDistance = _originalShadowDistance;
    }

    private static void ApplyPostProcessing(PlayerConfig c)
    {
        foreach (var camera in Camera.allCameras)
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = c.postProcessing;
    }

    // Brightness is a global volume with a post-exposure override on top of every other volume.
    private static void ApplyBrightness(PlayerConfig c)
    {
        if (_brightnessVolume == null)
        {
            var go = new GameObject("SettingsBrightness") { hideFlags = HideFlags.HideAndDontSave };
            _brightnessVolume = go.AddComponent<Volume>();
            _brightnessVolume.isGlobal = true;
            _brightnessVolume.priority = 1000f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _colorAdjustments = profile.Add<ColorAdjustments>(true);
            _brightnessVolume.sharedProfile = profile;
        }

        _colorAdjustments.postExposure.Override((c.brightness - 50) / 50f * 1.5f);
    }

    // Scales every CanvasScaler in the loaded scenes (popups made later are updated on the next change / scene load).
    private static void ApplyUiScale(PlayerConfig c)
    {
        float scale = Mathf.Clamp(c.uiScale, 50, 150) / 100f;

        foreach (var scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            int id = scaler.GetInstanceID();
            if (!UiBase.TryGetValue(id, out var baseValues))
            {
                baseValues = (scaler.referenceResolution, scaler.scaleFactor);
                UiBase[id] = baseValues;
            }

            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize) scaler.referenceResolution = baseValues.reference / scale;
            else if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize) scaler.scaleFactor = baseValues.scaleFactor * scale;
        }
    }

    public static void ApplyFocusMute(bool focused)
    {
        _focused = focused;
        var c = GameSettings.Config;
        AudioListener.volume = c != null && c.muteWhenUnfocused && !focused ? 0f : 1f;
    }
}