using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws the baker's SurfaceMesh (the NavMesh as a world-space mesh) with the MinimapGroundOverlay material.
/// The mesh is already in world space, so this object must stay at the scene root with identity transform.
/// Only revealed cells (MinimapExploredMask) are visible, everything else is clipped by the shader.
///
/// It also publishes the player's position and facing as the shader global _MinimapPlayerPos
/// (x, y = world X, Z of the player; z, w = facing X, Z) which the shader uses for the player icon.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MinimapOverlayRenderer : MonoBehaviour
{
    private static readonly int PlayerPosId = Shader.PropertyToID("_MinimapPlayerPos");
 
    [SerializeField] private MinimapMaskBaker baker;
    [SerializeField] private Transform target;
    [SerializeField] private PlayerController controller;
    [Tooltip("Optional. Transform whose forward is used for the icon arrow (e.g. the model child that rotates). " +
             "Falls back to the target.")]
    [SerializeField] private Transform facing;
 
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Vector3 lastDir = Vector3.forward;
    
    private InputHandler _inputHandler;
    private bool _subscribed;
 
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
        if(controller == null) controller = FindFirstObjectByType<PlayerController>();
        
        _inputHandler = controller != null ? controller.InputHandler : FindFirstObjectByType<InputHandler>();
 
        // a collider on the overlay would break click-to-move / raycasts
        if (TryGetComponent(out Collider col)) Destroy(col);
 
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
 
        // the mesh vertices are world-space positions
        if (transform.parent != null) transform.SetParent(null);
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one;
    }
 
    private void OnEnable()
    {
        if(_subscribed || _inputHandler == null || baker == null) return;
        _inputHandler.OnToggleMinimap += Toggle;
        baker.Baked += ApplyMesh;
        
        if (baker.IsBaked) ApplyMesh();
        _subscribed = true;
    }
 
    private void OnDisable()
    {
        if(_subscribed && _inputHandler != null) _inputHandler.OnToggleMinimap -= Toggle;
        if(baker != null) baker.Baked -= ApplyMesh;
        _subscribed = false;
    }
 
    private void ApplyMesh()
    {
        meshFilter.sharedMesh = baker.SurfaceMesh;
    }
 
    private void LateUpdate()
    {
        if (target == null) return;
 
        Vector3 p = target.position;
 
        Vector3 f = (facing != null ? facing : target).forward;
        f.y = 0f;
        if (f.sqrMagnitude > 1e-4f) lastDir = f.normalized;
 
        Shader.SetGlobalVector(PlayerPosId, new Vector4(p.x, p.z, lastDir.x, lastDir.z));
    }
}