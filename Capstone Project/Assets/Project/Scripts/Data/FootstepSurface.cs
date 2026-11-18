using UnityEngine;

[CreateAssetMenu(fileName = "NewFootstepSurface", menuName = "Audio/Footstep Surface")]
public class FootstepSurface : ScriptableObject
{
    [Tooltip("Played every stride. Put several clips in the cue so steps do not repeat.")]
    public AudioCue step;

    [Tooltip("Optional. Played when landing on this surface after being in the air. Empty = use the step cue.")]
    public AudioCue land;
}