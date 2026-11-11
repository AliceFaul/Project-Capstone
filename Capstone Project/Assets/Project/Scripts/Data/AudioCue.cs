using UnityEngine;
using Random = UnityEngine.Random;

public enum AudioChannel { Master, Music, Sfx }
public enum SfxBus { Gameplay, UI, Ambient }
public enum AudioSnapshotType { Normal, Combat, Paused }

[CreateAssetMenu(fileName = "New Audio Cue", menuName = "Audio/Audio Cue")]
public class AudioCue : ScriptableObject
{
    [Header("Clips")]
    public AudioClip[] clips;
    [Tooltip("With several clips, avoid playing the same one twice in a row.")]
    public bool avoidImmediateRepeat = true;
    
    [Header("Mix")]
    public SfxBus bus = SfxBus.Gameplay;
    [Range(0f, 1f)] public float volume = 1f;
    
    [Tooltip("Random multiplier applied to the volume on every play (min, max).")]
    public Vector2 volumeVariance = new Vector2(0.9f, 1f);

    [Tooltip("Random pitch on every play (min, max).)")]
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    
    [Header("Spatial (only when played at a position / following a transform).")]
    [Range(0f, 1f)] public float spatialBlend = 0.6f;

    [Tooltip("In a top-down view keep this >= camera-to-character distance, otherwise nearby sounds get quieter.")]
    public float minDistance = 10f;
    public float maxDistance = 40f;
    
    [Header("Limits")]
    [Tooltip("0 = most important, 256 = least. Used to decide what gets dropped when the voice pool is full.")]
    [Range(0, 256)] public int priority = 128;

    [Tooltip("Minium seconds (real time) between two plays of this cue.")]
    [Min(0f)] public float cooldown = 0.05f;

    [Tooltip("Max simultaneous voices for this cue.")]
    [Min(1)] public int maxInstances = 4;

    [Header("Credit (for the Credits screen)")]
    public string source;
    public string license;
    public string url;
    
    public bool HasClips => clips is { Length: > 0 };

    public AudioClip Pick(ref int lastIndex)
    {
        if(!HasClips) return null;

        if (clips.Length == 1)
        {
            lastIndex = 0;
            return clips[0];
        }

        int index = Random.Range(0, clips.Length);
        if(avoidImmediateRepeat && index == lastIndex) index = (index + 1) % clips.Length;

        lastIndex = index;
        return clips[index];
    }

    private void OnValidate()
    {
        volumeVariance.x = Mathf.Clamp01(volumeVariance.x);
        volumeVariance.y = Mathf.Clamp(volumeVariance.y, volumeVariance.x, 1f);

        pitchRange.x = Mathf.Max(0.1f, pitchRange.x);
        pitchRange.y = Mathf.Max(pitchRange.x, pitchRange.y);

        minDistance = Mathf.Max(0.01f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
    }
}