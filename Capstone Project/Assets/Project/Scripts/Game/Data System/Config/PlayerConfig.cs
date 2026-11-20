using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Config/Player")]
public class PlayerConfig : ScriptableObject, IConfig
{
    [Header("Graphic")]
    public int quality;
    public int fps;
    public bool activeCameraShake;
    // etc ...

    [Header("Display (settings screen)")]
    [Tooltip("Locale code (e.g. en, vi). Empty = the project default.")]
    public string language;
    
    [Tooltip("0 = fullscreen, 1 = borderless, 2 = windowed")]
    public int displayMode;
    
    [Tooltip("0 = the current screen resolution")]
    public int resolutionWidth;
    public int resolutionHeight;
    public bool vsync;
    
    [Tooltip("0 = off, 1 = low, 2 = medium, 3 = high (how far shadows are drawn)")]
    [Range(0, 3)] public int shadowQuality = 3;
    public bool postProcessing = true;
    [Range(0, 100)] public int brightness = 50;
    [Range(75, 125)] public int uiScale = 100;
    [Range(0f, 1f)] public float cameraShakeStrength = 1f;

    [Header("Audio")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public bool muteWhenUnfocused = true;

    [Header("Gameplay")]
    public bool showDamageText;
    public bool showFps;
    public bool tutorialHints = true;
    // etc ...

    [Header("Controller")]
    // switch keyboard, console, etc...

    [Header("Key Binding")]
    [Tooltip("Rebinds made in the Controls tab (Input System binding-override JSON). The KeyCode fields below are not used by it.")]
    [TextArea] public string bindingOverrides;
    public KeyCode inventory;
    public KeyCode minimap;
    public KeyCode heal;
    public KeyCode dash;
}