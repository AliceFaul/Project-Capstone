using UnityEngine;

[CreateAssetMenu(fileName = "RunConfig", menuName = "Run/Run Config")]
public class RunConfig : ScriptableObject
{
    [Header("Lives")]
    [Min(1)] public int maxLives = 3;
    [Tooltip("Fraction of max health the player comes back with.")]
    [Range(0.05f, 1f)] public float reviveHealthFraction = 1f;

    [Header("Death sequence (seconds, real time)")]
    [Tooltip("The model dissolves away.")]
    [Min(0.1f)] public float dissolveDuration = 1f;
    [Tooltip("When the particle burst plays during the dissolve: 0 = at the start, 1 = right after it ends.")]
    [Range(0f, 1f)] public float particleAtProgress = 0.5f;
    [Min(0f)] public float fadeToBlack = 0.4f;
    [Tooltip("Time on the black screen with the 'lives left' overlay (fade in, the lost life pops, hold, fade out). " +
             "About 1.5 s or more so the player can read it; the overlay never plays shorter than its own minimum.")]
    [Min(0f)] public float blackHold = 1.6f;

    [Header("Respawn sequence (seconds, real time)")]
    [Tooltip("The model materializes at the respawn point.")]
    [Min(0.1f)] public float materializeDuration = 0.8f;
    [Min(0f)] public float fadeFromBlack = 0.4f;

    [Header("Particles")]
    [Tooltip("Burst when the model dissolves.")]
    public GameObject deathParticle;
    [Tooltip("Burst when the model materializes. Empty = the death particle is reused.")]
    public GameObject respawnParticle;

    [Header("Audio")]
    [Tooltip("Plays at the player the moment he dies.")]
    public AudioCue deathCue;
    [Tooltip("Plays when the screen is black and a life has been lost.")]
    public AudioCue lifeLostCue;
    [Tooltip("Plays at the respawn point.")]
    public AudioCue respawnCue;
    [Tooltip("Music while the player is dead. The scene music fades out into it and comes back after the respawn.")]
    public MusicTrack deathTrack;
    [Tooltip("Music when the run is lost (no lives left).")]
    public MusicTrack defeatTrack;
    [Tooltip("Music when the run is won (boss cleared).")]
    public MusicTrack victoryTrack;
    [Tooltip("Crossfade length for the tracks above.")]
    [Min(0f)] public float musicFade = 1f;
}