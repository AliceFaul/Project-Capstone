using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [SerializeField] private MusicTrack track;

    [Tooltip("Crossfade seconds. Negative = track / AudioConfig default.")]
    [SerializeField] private float fade = -1f;
    [SerializeField] private bool playOnStart = true;

    private void Start()
    {
        if(playOnStart) Play();
    }

    // Empty track = do nothing
    public void Play()
    {
        if(track == null) return;
        AudioService.Instance?.PlayMusic(track, fade);
    }

    // For map / level switching. Crossfades to the new track. Passing null fades the base music out.
    public void SetTrack(MusicTrack newTrack, float fadeSeconds = -1f)
    {
        track = newTrack;
        AudioService.Instance?.PlayMusic(newTrack, fadeSeconds);
    }
}