using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Miniature minimap lying flat at the target's feet (no more ground-hugging).
///
/// The whole map (MinimapExploredMask) is shrunk by "mapScale" and drawn on a flat quad that follows the
/// target and is centered on it. The shader maps every point of the quad back to the real world position
/// it represents, so the miniature "slides" under the target as it moves and the player icon always sits
/// at the target's feet, on top of the spot of the map where the player really is.
///
/// Publishes two shader globals:
///   _MinimapPlayerPos  : (x, y = world X, Z of the target; z, w = facing X, Z)
///   _MinimapMiniParams : (mapScale, visibleRadius, 0, 0)
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MinimapOverlayRenderer : MonoBehaviour
{
    private static readonly int PlayerPosId = Shader.PropertyToID("_MinimapPlayerPos");
    private static readonly int MiniParamsId = Shader.PropertyToID("_MinimapMiniParams");

    [Header("References")]
    [SerializeField] private MinimapMaskBaker baker;
    [SerializeField] private Transform target;
    [Tooltip("Optional. Transform whose forward is used for the icon arrow (e.g. the model child that rotates). " +
             "Falls back to the target.")]
    [SerializeField] private Transform facing;

    [Header("Miniature")]
    [Tooltip("Radius (meters, on the ground) of the circle the miniature occupies around the target.")]
    [SerializeField, Min(1f)] private float visibleRadius = 8f;

    [Tooltip("Miniature ratio = screen meters per real meter. 0.1 means a 3 m wide path is 0.3 m wide on screen.")]
    [SerializeField, Min(0.005f)] private float mapScale = 0.1f;

    [Tooltip("Ignore Map Scale and shrink the level until every point of it fits inside the circle, " +
             "wherever the target stands. Paths get small on big levels.")]
    [SerializeField] private bool fitWholeLevel = false;

    [Tooltip("Lift (meters) above the target's feet.")]
    [SerializeField, Min(0f)] private float heightOffset = 0.05f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh quad;
    private Vector3 lastDir = Vector3.forward;

    public float CurrentScale { get; private set; }
    public bool IsVisible => meshRenderer != null && meshRenderer.enabled;

    public void SetVisible(bool visible)
    {
        if (meshRenderer != null) meshRenderer.enabled = visible;
    }

    public void Toggle() => SetVisible(!IsVisible);

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // a collider on the overlay would break click-to-move / raycasts
        if (TryGetComponent(out Collider col)) Destroy(col);

        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        // position / scale are driven in world space every frame
        if (transform.parent != null) transform.SetParent(null);
        transform.rotation = Quaternion.identity;

        quad = BuildQuad();
        meshFilter.sharedMesh = quad;
    }

    private void OnDestroy()
    {
        if (quad != null) Destroy(quad);
    }

    private static Mesh BuildQuad()
    {
        // 1 x 1 flat quad on the XZ plane, centered on the origin
        var m = new Mesh { name = "MinimapMiniQuad" };
        m.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f, -0.5f)
        };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.bounds = new Bounds(Vector3.zero, new Vector3(1f, 0.1f, 1f));
        return m;
    }

    private float ComputeScale()
    {
        if (fitWholeLevel && baker != null && baker.IsBaked)
        {
            float diagonal = baker.WorldSize.magnitude;
            if (diagonal > 0.01f) return visibleRadius / diagonal;
        }
        return mapScale;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 p = target.position;
        CurrentScale = ComputeScale();

        transform.position = new Vector3(p.x, p.y + heightOffset, p.z);
        transform.localScale = new Vector3(visibleRadius * 2f, 1f, visibleRadius * 2f);

        Vector3 f = (facing != null ? facing : target).forward;
        f.y = 0f;
        if (f.sqrMagnitude > 1e-4f) lastDir = f.normalized;

        Shader.SetGlobalVector(PlayerPosId, new Vector4(p.x, p.z, lastDir.x, lastDir.z));
        Shader.SetGlobalVector(MiniParamsId, new Vector4(CurrentScale, visibleRadius, 0f, 0f));
    }
}