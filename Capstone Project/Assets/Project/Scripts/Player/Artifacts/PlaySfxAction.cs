using System;
using System.Collections;
using UnityEngine;

// A sound as one step of an artifact. Add as many as needed to ArtifactData.actions (it shows up in the
// dropdown like every other action): charge-up on key press, cast whoosh, impact, heal chime, ...
// No change to ArtifactData is needed.
[Serializable]
public class PlaySfxAction : ArtifactAction
{
    public enum Timing
    {
        OnExecute,
        OnUse
    }

    public enum Anchor
    {
        Caster,
        AimPoint,
        Origin
    }

    public AudioCue cue;
    [Tooltip("OnExecute: when the actions run (after executeTime). OnUse: the moment the key is pressed, before castTime (charge-up sound).")]
    public Timing timing = Timing.OnExecute;
    [Tooltip("Where the sound comes from. AimPoint = the spot under the mouse, Origin = the cast origin (e.g. firePoint).")]
    public Anchor at = Anchor.Caster;
    [Tooltip("Seconds to wait before playing, e.g. to line up with a falling VFX.")]
    [Min(0f)] public float delay;

    // ArtifactController plays OnUse sounds itself when the artifact is used.
    public bool PlaysOnUse => timing == Timing.OnUse;

    public override void Execute(ArtifactContext context)
    {
        if (timing == Timing.OnUse) return; // already played by the controller
        Play(context);
    }

    public void Play(ArtifactContext context)
    {
        if (cue == null || context == null) return;

        Vector3 position = GetPosition(context);

        if (delay <= 0f) AudioService.Instance?.PlaySfx(cue, position);
        else if (context.Runner != null) context.Runner.StartCoroutine(PlayAfterDelay(position));
    }

    private IEnumerator PlayAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(delay);
        AudioService.Instance?.PlaySfx(cue, position);
    }

    private Vector3 GetPosition(ArtifactContext context)
    {
        switch (at)
        {
            case Anchor.AimPoint: return context.AimPoint;
            case Anchor.Origin: return context.Origin;
            default: return context.CasterTransform != null ? context.CasterTransform.position : context.Origin;
        }
    }
}
