using UnityEngine;

/// <summary>
/// One kind of minimap icon (enemy, NPC, chest, quest objective...)
/// </summary>
[CreateAssetMenu(fileName = "New Minimap Marker Type", menuName = "Minimap/Marker Type")]
public class MinimapMarkerType : ScriptableObject
{
    [Tooltip("Use a sprite with Mesh Type = Full Rect.")]
    public Sprite sprite;
    
    public Color color = Color.white;
    
    [Tooltip("Icon size in SCREEN meters (the miniature's own scale, not real world meters).")]
    [Min(0.02f)] public float size = 0.3f;

    [Tooltip("Only show once the map cell under the target has been revealed (fog of war).")]
    public bool requireRevealed = false;

    [Tooltip("When the target is outside the miniature circle, stick to its edge instead of hiding (quest objectives).")]
    public bool clampToEdge = false;

    [Tooltip("Gentle size pulse to draw attension.")]
    public bool pulse = false;

    [Tooltip("Higher draws on top of lower.")]
    public int priority = 0;
}