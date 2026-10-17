using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Puts on a Cube (with the MinimapGroundOverlay material) and keeps it centered on the target.
/// The box is the volume in which the overlay is allowed to appear:
///     - horizontally: a square of "size" meters around the target
///     - vertically: from (feet - heightBelow) to (feet + heightAbove)
/// It also publishes the player's position and facing as the shader global _MinimapPlayerPos
/// (x, y = world X, Z of the player; z, w = facing X, Z) which the shader uses
/// for the player icon.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class MinimapOverlayProjector : MonoBehaviour
{
    private static readonly int PlayerPosId = Shader.PropertyToID("_MinimapPlayerPos");
    
    [SerializeField] private Transform target;
    [Tooltip("Optional. Transform whose forward is used for the icon arrow (e.g. the model child that rotates). " + 
             "Falls back to the target.")]
    [SerializeField] private Transform facing;
    
    [Tooltip("Diameter (meters) of the visible circle around the target.")]
    [SerializeField, Min(5f)] private float size = 18f;
    [Tooltip("How far below the target's feet the overlay may reach (slopes, stairs).")]
    [SerializeField, Min(0.1f)] private float heightBelow = 1.5f;
    [Tooltip("How far above the target's feet the overlay may reach.")]
    [SerializeField, Min(0.05f)] private float heightAbove = 0.35f;
    
    private Vector3 lastDir = Vector3.forward;

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
        
        Vector3 f = (facing != null ? facing : target).forward;
        f.y = 0f;
        if(f.sqrMagnitude > 1e-4f) lastDir = f.normalized;
        
        Shader.SetGlobalVector(PlayerPosId, new Vector4(p.x, p.z, lastDir.x, lastDir.z));
    }
}