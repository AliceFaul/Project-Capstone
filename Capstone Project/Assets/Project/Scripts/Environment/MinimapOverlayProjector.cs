using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Puts on a Cube (with the MinimapGroundOverlay material) and keeps it centered on the target.
/// The box is the volume in which the overlay is allowed to appear:
///     - horizontally: a square of "size" meters around the target
///     - vertically: from (feet - heightBelow) to (feet + heightAbove)
/// Keep heightAbove small so the player's body / props are not painted.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class MinimapOverlayProjector : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Tooltip("Horizontal size (meters). Should be a bit larger than what the camera sees.")]
    [SerializeField, Min(5f)] private float size = 40f;
    [Tooltip("How far below the target's feet the overlay may reach (slopes, stairs).")]
    [SerializeField, Min(0.1f)] private float heightBelow = 1.5f;
    [Tooltip("How far above the target's feet the overlay may reach.")]
    [SerializeField, Min(0.05f)] private float heightAbove = 0.35f;

    private void Awake()
    {
        // a collider on the overlay box would break click-to-move / raycasts
        if(TryGetComponent(out Collider col)) Destroy(col);
        
        var mr = GetComponent<MeshRenderer>();
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        
        transform.rotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        if(target == null) return;
        
        Vector3 p = target.position;
        float height = heightBelow + heightAbove;

        transform.position = new Vector3(p.x, p.y + (heightAbove - heightBelow) * 0.5f, p.z);
        transform.localScale = new Vector3(size, height, size);
    }
}