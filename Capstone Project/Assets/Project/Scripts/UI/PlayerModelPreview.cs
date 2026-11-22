using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class PlayerModelPreview : MonoBehaviour, IDragHandler
{
    [SerializeField] private RawImage target;
    
    [Tooltip("Optional. A model-only prefab of the player. Empty = copy the model that is under the player in the scene.")]
    [SerializeField] private GameObject modelPrefab;
    
    [Tooltip("Layer used by the copy and the preview camera. Untick it in the Main Camera's culling mask (optional).")]
    [Range(0, 31)] [SerializeField] private int previewLayer = 31;
    [SerializeField] private Vector3 origin = new Vector3(0f, -1000f, 0f);
    [SerializeField] private Vector2Int textureSize = new Vector2Int(512, 1024);
    [SerializeField] private float fieldOfView = 22f;
    
    [Tooltip("Camera height as a fraction of the model height (0.5 = middle).")]
    [Range(0.3f, 0.8f)] [SerializeField] private float lookAtHeight = 0.5f;
    [SerializeField] private float swayDegrees = 20f;
    [SerializeField] private float dragSpeed = 0.5f;

    private Camera _camera;
    private RenderTexture _texture;
    private GameObject _clone;
    private float _dragYaw;
    private float _lastDragTime = -10f;

    public void Show()
    {
        Hide();
        EnsureCamera();
        BuildClone();
        if (_clone == null) return;

        Frame();
        _dragYaw = 0f;
        _camera.enabled = true;
        target.texture = _texture;
    }

    public void Hide()
    {
        if (_camera != null) _camera.enabled = false;
        if (_clone != null) Destroy(_clone);
        _clone = null;
    }

    private void OnDestroy()
    {
        Hide();
        if (_camera != null) Destroy(_camera.gameObject);
        if (_texture != null)
        {
            _texture.Release();
            Destroy(_texture);
        }
    }

    private void Update()
    {
        if (_clone == null) return;

        float sway = Time.unscaledTime - _lastDragTime > 2f ? Mathf.Sin(Time.unscaledTime * 0.9f) * swayDegrees : 0f;
        _clone.transform.rotation = Quaternion.Euler(0f, _dragYaw + sway, 0f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _dragYaw -= eventData.delta.x * dragSpeed;
        _lastDragTime = Time.unscaledTime;
    }

    private void EnsureCamera()
    {
        if (_texture == null)
        {
            _texture = new RenderTexture(textureSize.x, textureSize.y, 24, RenderTextureFormat.ARGB32) { name = "PlayerModelPreview" };
        }
        if (_camera != null) return;

        var go = new GameObject("PlayerModelPreviewCamera");
        _camera = go.AddComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        _camera.cullingMask = 1 << previewLayer;
        _camera.fieldOfView = fieldOfView;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 60f;
        _camera.depth = -50f;
        _camera.allowHDR = false;
        _camera.targetTexture = _texture;
        _camera.enabled = false;

        var data = _camera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = false;
        data.renderShadows = true;
    }

    private void BuildClone()
    {
        if (modelPrefab != null)
        {
            _clone = Instantiate(modelPrefab, origin, Quaternion.identity);
        }
        else
        {
            var player = FindFirstObjectByType<PlayerController>();
            var animator = player != null ? player.GetComponentInChildren<Animator>() : null;
            if (animator == null)
            {
                Debug.LogWarning("[PlayerModelPreview] No model found: assign 'modelPrefab' (or put the player's Animator on a child object).");
                return;
            }
            if (animator.gameObject == player.gameObject)
            {
                Debug.LogWarning("[PlayerModelPreview] The Animator is on the player root, which has gameplay scripts: assign a model-only prefab to 'modelPrefab'.");
                return;
            }

            // copy it while it is inactive so none of its scripts wake up in the copy
            var source = animator.gameObject;
            bool wasActive = source.activeSelf;
            source.SetActive(false);
            _clone = Instantiate(source, origin, Quaternion.identity);
            source.SetActive(wasActive);
        }

        _clone.SetActive(false);
        Strip(_clone);
        _clone.SetActive(true);

        foreach (var child in _clone.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = previewLayer;

        var animatorCopy = _clone.GetComponentInChildren<Animator>();
        if (animatorCopy != null)
        {
            animatorCopy.updateMode = AnimatorUpdateMode.UnscaledTime; // the game is paused (timeScale 0)
            animatorCopy.applyRootMotion = false;
            animatorCopy.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    // Keeps only what is needed to draw it: transforms, mesh renderers and the animator.
    private static void Strip(GameObject root)
    {
        foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null) DestroyImmediate(behaviour);
        }
        foreach (var renderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true)) DestroyImmediate(renderer);
        foreach (var renderer in root.GetComponentsInChildren<TrailRenderer>(true)) DestroyImmediate(renderer);
        foreach (var renderer in root.GetComponentsInChildren<LineRenderer>(true)) DestroyImmediate(renderer);

        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            if (component is Transform || component is Animator || component is MeshFilter || component is MeshRenderer || component is SkinnedMeshRenderer) continue;
            DestroyImmediate(component);
        }
    }

    // The camera stands in front of the model (the model faces +Z) at a distance that fits its height.
    private void Frame()
    {
        var renderers = _clone.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);

        float height = Mathf.Max(bounds.size.y, bounds.size.x * 1.3f, 0.5f);
        float distance = height * 0.62f / Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        var lookAt = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * lookAtHeight, bounds.center.z);

        _camera.transform.position = lookAt + new Vector3(0f, height * 0.04f, distance);
        _camera.transform.LookAt(lookAt);
    }
}