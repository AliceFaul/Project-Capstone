using UnityEngine;

public enum MusicPriority
{
    Scene = 0,
    Combat = 100,
    Boss = 200,
    Cinematic = 300
}

[CreateAssetMenu(fileName = "NewMusicTrack", menuName = "Audio/Music Track")]
public class MusicTrack : ScriptableObject
{
    [Header("Clips")]
    [Tooltip("Use OGG/WAV. MP3 often leaves a gap when looping. Import as Streaming.")]
    public AudioClip clip;
    public bool loop = true;
    
    [Header("Mix")]
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Crossfade seconds used when this track is started without an explicit fade. Negative = AudioConfig default.")]
    public float fadeOverride = -1f;

    [Header("Behaviour")]
    [Tooltip(
        "When another track takes over and later this one returns, continue from where it stopped instead of restarting. " +
        "Good for exploration music, usually off for combat music.")]
    public bool resumeOnReturn = false;

    [Header("Credit (for the Credits screen).")]
    public string source;
    public string license;
    public string url;
}