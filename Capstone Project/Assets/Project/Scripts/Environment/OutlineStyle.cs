using UnityEngine;

public enum OutlineOcclusion
{
    /// <summary> Outline only around the part of the object that is actually visible. </summary>
    VisibleOnly,
    
    /// <summary> Outline around the whole silhouette, also through walls / trees (full strength). </summary>
    XRay,
    
    /// <summary> Visible part at full strength, hidden part faded to Ghost Opacity. </summary>
    XRayGhost
}

/// <summary>
/// Look of one kind of outline (enemy, NPC, interactable...).
/// Create assets in Asset Menu and assign them to OutlineTarget.
/// </summary>
[CreateAssetMenu(fileName = "New Outline Style", menuName = "Outline/Outline Style")]
public class OutlineStyle : ScriptableObject
{
    [Header("Look")]
    [ColorUsage(false, true)] public Color color = Color.white;
    
    [Header("Occlusion")]
    [Tooltip("What happens when the object is partly or fully hidden behind something.")]
    public OutlineOcclusion occlusion = OutlineOcclusion.VisibleOnly;
    
    [Tooltip("Only used by XRayGhost.")]
    [Range(0.05f, 1f)] public float ghostOpacity = 0.35f;
    
    [Header("Fade (seconds)")]
    [Min(0f)] public float fadeInTime = 0.08f;
    [Min(0f)] public float fadeOutTime = 0.12f;

    /// <summary> Opacity (0..1) of the outline on the hidden part of the object. </summary>
    public float HiddenOpacity
    {
        get
        {
            switch (occlusion)
            {
                case OutlineOcclusion.XRay: return 1f;
                case OutlineOcclusion.XRayGhost: return ghostOpacity;
                default: return 0f;
            }
        }
    }
}