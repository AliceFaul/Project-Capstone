using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Config/Player")]
public class PlayerConfig : ScriptableObject, IConfig
{
    [Header("Graphic")]
    public int quality;
    public int fps;
    public bool activeCameraShake;
    // etc ...
    
    [Header("Audio")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Gameplay")] 
    public bool showDamageText;
    // etc ...
    
    [Header("Controller")]
    // switch keyboard, console, etc...
    
    [Header("Key Binding")]
    public KeyCode inventory;
    public KeyCode minimap;
    public KeyCode heal;
    public KeyCode dash;
}
