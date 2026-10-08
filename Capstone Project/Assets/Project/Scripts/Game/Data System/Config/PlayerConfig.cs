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
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;

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
