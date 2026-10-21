using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws an icon for every active MinimapMarker on the miniature map.
///
/// Lives on the "Icons" child of the MinimapSystem prefab and is driven by MinimapOverlayRenderer
/// (it calls Refresh at the end of its LateUpdate, so icons and map never lag one frame behind each other).
/// Each icon is a small quad that faces the camera, placed at
///     quadCenter + (markerXZ - playerXZ) * mapScale
/// Icons are always drawn on top (no depth test), after the map.
///
/// The fog-of-war mask and the camera are found automatically (MinimapSystem / Camera.main).
/// </summary>
public class MinimapIconLayer : MonoBehaviour
{
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int UVRectId = Shader.PropertyToID("_UVRect");

    [Header("Assets")]
    [Tooltip("Material using the shader Mhieu/Minimap/Icon.")]
    [SerializeField] private Material iconMaterial;

    [Header("Look")]
    [Tooltip("Icons fade out over this fraction of the radius near the edge of the circle.")]
    [SerializeField, Range(0.01f, 1f)] private float edgeFade = 0.2f;
    [Tooltip("Pulse speed (cycles per second) for marker types with Pulse enabled.")]
    [SerializeField, Min(0.1f)] private float pulseSpeed = 1.5f;

    private sealed class Slot
    {
        public Transform tf;
        public MeshRenderer mr;
        public MaterialPropertyBlock mpb;
        public MinimapMarkerType type;
    }

    private readonly List<Slot> slots = new List<Slot>();
    private readonly Dictionary<Sprite, Vector4> uvCache = new Dictionary<Sprite, Vector4>();
    private MinimapController _controller;
    private Mesh quad;
    private bool visible = true;

    public void SetVisible(bool value)
    {
        if (visible == value) return;
        visible = value;
        if (!visible) HideFrom(0);
    }

    private void Awake()
    {
        _controller = GetComponentInParent<MinimapController>(true);
        quad = BuildQuad();
    }

    private void OnDestroy()
    {
        if (quad != null) Destroy(quad);
    }

    /// <summary>
    /// Called by MinimapOverlayRenderer every frame.
    /// center = center of the miniature quad (the player's feet), scale = mapScale, radius = visibleRadius.
    /// </summary>
    public void Refresh(Vector3 center, float scale, float radius)
    {
        if (!visible || iconMaterial == null) return;

        MinimapExploredMask explored = _controller != null ? _controller.Explored : null;
        Camera cam = Camera.main;
        Quaternion rot = cam != null ? cam.transform.rotation : Quaternion.identity;

        Vector2 origin = new Vector2(center.x, center.z);
        float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
        float parentScale = Mathf.Max(transform.lossyScale.x, 1e-4f);

        int used = 0;
        IReadOnlyList<MinimapMarker> markers = MinimapMarker.All;

        for (int i = 0; i < markers.Count; i++)
        {
            MinimapMarker m = markers[i];
            MinimapMarkerType t = m.Type;
            if (t == null || t.sprite == null) continue;

            Vector3 wp = m.WorldPosition;
            if (t.requireRevealed && explored != null && !explored.IsRevealed(wp)) continue;

            Vector2 offset = (new Vector2(wp.x, wp.z) - origin) * scale;
            float dist = offset.magnitude;
            float alpha = 1f;

            if (dist > radius)
            {
                if (!t.clampToEdge) continue;
                offset *= radius * 0.92f / dist;   // stick to the edge, a bit inside so it stays fully visible
            }
            else if (!t.clampToEdge)
            {
                alpha = 1f - Mathf.InverseLerp(radius * (1f - edgeFade), radius, dist);
                if (alpha <= 0.01f) continue;
            }

            Slot s = GetSlot(used++);
            if (s.type != t) ApplyType(s, t);

            s.mr.enabled = true;
            s.mr.sortingOrder = t.priority;
            s.tf.SetPositionAndRotation(new Vector3(center.x + offset.x, center.y, center.z + offset.y), rot);
            s.tf.localScale = Vector3.one * (t.size * (t.pulse ? pulse : 1f) / parentScale);

            Color c = t.color;
            c.a *= alpha;
            s.mpb.SetColor(ColorId, c);
            s.mr.SetPropertyBlock(s.mpb);
        }

        HideFrom(used);
    }

    private void HideFrom(int index)
    {
        for (int i = index; i < slots.Count; i++) slots[i].mr.enabled = false;
    }

    private Slot GetSlot(int index)
    {
        while (slots.Count <= index)
        {
            var go = new GameObject("MinimapIcon");
            go.transform.SetParent(transform, false);

            go.AddComponent<MeshFilter>().sharedMesh = quad;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = iconMaterial;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            slots.Add(new Slot { tf = go.transform, mr = mr, mpb = new MaterialPropertyBlock() });
        }

        return slots[index];
    }

    private void ApplyType(Slot s, MinimapMarkerType t)
    {
        s.mpb.SetTexture(MainTexId, t.sprite.texture);
        s.mpb.SetVector(UVRectId, GetUVRect(t.sprite));
        s.type = t;
    }

    // normalized rectangle of the sprite inside its texture (also works when the sprite is packed in an atlas)
    private Vector4 GetUVRect(Sprite sprite)
    {
        if (uvCache.TryGetValue(sprite, out Vector4 cached)) return cached;

        Vector2[] uv = sprite.uv;
        float minU = float.MaxValue, minV = float.MaxValue, maxU = float.MinValue, maxV = float.MinValue;
        for (int i = 0; i < uv.Length; i++)
        {
            if (uv[i].x < minU) minU = uv[i].x;
            if (uv[i].x > maxU) maxU = uv[i].x;
            if (uv[i].y < minV) minV = uv[i].y;
            if (uv[i].y > maxV) maxV = uv[i].y;
        }

        var rect = new Vector4(minU, minV, maxU - minU, maxV - minV);
        uvCache[sprite] = rect;
        return rect;
    }

    private static Mesh BuildQuad()
    {
        // 1 x 1 quad on the XY plane, centered on the origin
        var m = new Mesh { name = "MinimapIconQuad" };
        m.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f)
        };
        m.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.bounds = new Bounds(Vector3.zero, new Vector3(1f, 1f, 0.1f));
        return m;
    }
}