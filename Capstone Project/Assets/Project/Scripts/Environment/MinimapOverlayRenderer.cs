using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MinimapOverlayRenderer : MonoBehaviour
{
    private static readonly int PlayerPosId = Shader.PropertyToID("_MinimapPlayerPos");
    private static readonly int MiniParamsId = Shader.PropertyToID("_MinimapMiniParams");

    [Header("Miniature")]
    [Tooltip("Radius (meters, on the ground) of the circle the miniature occupies around the target.")]
    [SerializeField, Min(1f)] private float visibleRadius = 8f;

    [Tooltip("Miniature ratio = screen meters per real meter. 0.1 means a 3 m wide path is 0.3 m wide on screen.")]
    [SerializeField, Min(0.005f)] private float mapScale = 0.1f;

    [Tooltip("Ignore Map Scale and shrink the level until every point of it fits inside the circle, " +
             "wherever the target stands. Paths get small on big levels.")]
    [SerializeField] private bool fitWholeLevel = false;

    [Tooltip("While the minimap is limited to an area (runs): shrink the miniature so the whole area fits the circle, " +
             "wherever the target stands. Off = keep Map Scale.")]
    [SerializeField] private bool fitToActiveArea = false;

    [Tooltip("Lift (meters) above the target's feet.")]
    [SerializeField, Min(0f)] private float heightOffset = 0.05f;

    private MinimapController _controller;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh quad;
    private Vector3 lastDir = Vector3.forward;
    private bool userVisible = true;

    public float CurrentScale { get; private set; }
    public bool IsVisible => userVisible;

    public void SetVisible(bool visible)
    {
        userVisible = visible;
    }

    public void Toggle() => SetVisible(!userVisible);

    private void Awake()
    {
        _controller = GetComponentInParent<MinimapController>(true);
        if(_controller == null) Debug.LogError("[MinimapOverlayRenderer] Needs a MinimapSystem on a parent object.", this);

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // a collider on the overlay would break click-to-move / raycasts
        if(TryGetComponent(out Collider col)) Destroy(col);

        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        quad = BuildQuad();
        meshFilter.sharedMesh = quad;
    }

    private void OnDestroy()
    {
        if(quad != null) Destroy(quad);
    }

    private static Mesh BuildQuad()
    {
        // 1 x 1 flat quad on the XZ plane, centered on the origin
        var m = new Mesh
        {
            name = "MinimapMiniQuad",
            vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f,  0.5f),
                new Vector3( 0.5f, 0f,  0.5f),
                new Vector3( 0.5f, 0f, -0.5f)
            },
            triangles = new[] { 0, 1, 2, 0, 2, 3 },
            bounds = new Bounds(Vector3.zero, new Vector3(1f, 0.1f, 1f))
        };
        return m;
    }

    private float ComputeScale()
    {
        if (fitToActiveArea && _controller.HasAreaWindow)
            return visibleRadius / Mathf.Max(0.01f, _controller.AreaWindowRadius * 2f);

        MinimapMaskBaker baker = _controller.Baker;
        if (fitWholeLevel && baker != null && baker.IsBaked)
        {
            float diagonal = baker.WorldSize.magnitude;
            if (diagonal > 0.01f) return visibleRadius / diagonal;
        }
        return mapScale;
    }

    // the quad is driven in WORLD space, so a scaled parent must not change its size
    private void SetWorldDiameter(float diameter)
    {
        Vector3 ps = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(
            diameter / Mathf.Max(ps.x, 1e-4f),
            1f,
            diameter / Mathf.Max(ps.z, 1e-4f));
    }

    private void LateUpdate()
    {
        if (_controller == null) return;

        Transform target = _controller.Target;
        bool show = userVisible && target != null;

        if (meshRenderer.enabled != show) meshRenderer.enabled = show;

        MinimapIconLayer icons = _controller.Icons;
        if (icons != null) icons.SetVisible(show);

        if (target == null) return;

        Vector3 p = target.position;
        CurrentScale = ComputeScale();

        transform.rotation = Quaternion.identity;
        transform.position = new Vector3(p.x, p.y + heightOffset, p.z);
        SetWorldDiameter(visibleRadius * 2f);

        Transform facingTransform = _controller.Facing != null ? _controller.Facing : target;
        Vector3 f = facingTransform.forward;
        f.y = 0f;
        if (f.sqrMagnitude > 1e-4f) lastDir = f.normalized;

        Shader.SetGlobalVector(PlayerPosId, new Vector4(p.x, p.z, lastDir.x, lastDir.z));
        Shader.SetGlobalVector(MiniParamsId, new Vector4(CurrentScale, visibleRadius, 0f, 0f));

        // after the quad moved, so icons and map never lag one frame behind each other
        if (show && icons != null) icons.Refresh(transform.position, CurrentScale, visibleRadius);
    }
}