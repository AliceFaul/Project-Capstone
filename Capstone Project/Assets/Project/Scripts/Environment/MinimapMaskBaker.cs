using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// Rasterizes the NavMesh triangulation (top-down, XZ plane) into an R8 texture
/// ("walkable mask") and publishes it, together with its world rect, as shader globals:
/// _MinimapWalkableMask : Texture2D (255 = walkable, 0 = not)
/// _MinimapWorldRect    : Vector4 (originX, originZ, sizeX, sizeZ) in world units
/// UV of a world point = ((x - originX) / sizeX, (z - originZ) / sizeZ)
/// </summary>
public class MinimapMaskBaker : MonoBehaviour
{
    private static readonly int WalkableMaskId = Shader.PropertyToID("_MinimapWalkableMask");
    private static readonly int WorldRectId = Shader.PropertyToID("_MinimapWorldRect");
    
    [Header("Bake Settings")]
    [Tooltip("World size of one texel. Bigger = chunkier.")]
    [SerializeField, Min(0.05f)] private float metersPerTexel = 0.5f;
    [Tooltip("Extra margin (meters) around the NavMesh bounds.")]
    [SerializeField, Min(0f)] private float padding = 2f;
    [Tooltip("Safety clamp for the largest texture dimension.")]
    [SerializeField, Min(16)] private int maxResolution = 2048;
    [SerializeField] private bool bakeOnStart = true;
    
    [Header("Debug (Optional)")]
    [Tooltip("Assign any RawImage to preview the baked mask while testing.")]
    [SerializeField] private RawImage debugPreview;
    
    public Texture2D WalkableMask { get; private set; }
    public Vector2 WorldOrigin { get; private set; } // (x, z)
    public Vector2 WorldSize { get; private set; } // (x, z)
    public float MetersPerTexel { get; private set; }
    public bool IsBaked => WalkableMask != null;

    public event Action Baked;

    [ContextMenu("Bake")]
    public void Bake()
    {
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        if (tri.vertices.Length == 0 || tri.indices.Length < 3)
        {
            Debug.LogWarning($"[MinimapMaskBaker] No NavMesh data found. Bake the NavMesh first and come back!");
            return;
        }
        
        // 1. Bounds on the XZ plane
        float minX = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxZ = float.MinValue;
        foreach (var v in tri.vertices)
        {
            if (v.x < minX) minX = v.x;
            if (v.x > maxX) maxX = v.x;
            if (v.z < minZ) minZ = v.z;
            if (v.z > maxZ) maxZ = v.z;
        }
        minX -= padding; minZ -= padding;
        maxX += padding; maxZ += padding;
        
        // 2. Resolution (clamped by maxResolution)
        float mpt = metersPerTexel;
        int width = Mathf.Max(1, Mathf.CeilToInt((maxX - minX) / mpt));
        int height = Mathf.Max(1, Mathf.CeilToInt((maxZ - minZ) / mpt));

        int maxDim = Mathf.Max(width, height);
        if (maxDim > maxResolution)
        {
            mpt *= (float)maxDim / maxResolution;
            width = Mathf.Max(1, Mathf.CeilToInt((maxX - minX) / mpt));
            height = Mathf.Max(1, Mathf.CeilToInt((maxZ - minZ) / mpt));
        }

        MetersPerTexel = mpt;
        WorldOrigin = new Vector2(minX, minZ);
        WorldSize = new Vector2(width * mpt, height * mpt);
        
        // 3. Rasterize every NavMesh triangle
        byte[] data = new byte[width * height];
        int[] idx = tri.indices;
        Vector3[] verts = tri.vertices;

        for (int i = 0; i + 2 < idx.Length; i += 3)
        {
            Vector2 a = ToTexel(verts[idx[i]]);
            Vector2 b = ToTexel(verts[idx[i + 1]]);
            Vector2 c = ToTexel(verts[idx[i + 2]]);
            RasterizeTriangle(data, width, height, a, b, c);
        }
        
        // 4. Upload
        if(WalkableMask != null) Destroy(WalkableMask);

        WalkableMask = new Texture2D(width, height, TextureFormat.R8, false, true)
        {
            name = "MinimapWalkableMask",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        WalkableMask.SetPixelData(data, 0);
        WalkableMask.Apply(false, false); // keep readable
        
        // 5. Publish to shaders
        Shader.SetGlobalTexture(WalkableMaskId, WalkableMask);
        Shader.SetGlobalVector(WorldRectId, new Vector4(WorldOrigin.x, WorldOrigin.y, WorldSize.x, WorldSize.y));
        
        if(debugPreview != null) debugPreview.texture = WalkableMask;
        
        Debug.Log($"[MinimapMaskBaker] Baked {width}x{height} mask, {mpt:0.###} m/texel, " + 
                  $"world rect origin={WorldOrigin} size={WorldSize}");

        Baked?.Invoke();
    }

    // ---------- Helpers ----------
    
    public Vector2 WorldToUV(Vector3 worldPos)
    {
        return new Vector2(
            (worldPos.x - WorldOrigin.x) / WorldSize.x,
            (worldPos.z - WorldOrigin.y) / WorldSize.y);
    }

    public Vector2Int WorldToTexel(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPos.x - WorldOrigin.x) / MetersPerTexel),
            Mathf.FloorToInt((worldPos.z - WorldOrigin.y) / MetersPerTexel));
    }

    private Vector2 ToTexel(Vector3 worldPos)
    {
        return new Vector2(
            (worldPos.x - WorldOrigin.x) / MetersPerTexel,
            (worldPos.z - WorldOrigin.y) / MetersPerTexel);
    }
    
    private static float Edge(Vector2 a, Vector2 b, Vector2 p)
    {
        return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    }

    private static void RasterizeTriangle(byte[] data, int width, int height, Vector2 a, Vector2 b, Vector2 c)
    {
        float area = Edge(a, b, c);
        if(Mathf.Abs(area) < 1e-6f) return;
        float invArea = 1f / area;

        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)));
        int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)));
        
        // small tolerance so thin triangles / shared edges don't leave holes
        const float eps = 0.02f;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                
                float w0 = Edge(b, c, p) * invArea;
                float w1 = Edge(c, a, p) * invArea;
                float w2 = Edge(a, b, p) * invArea;

                if(w0 >= -eps && w1 >= -eps && w2 >= -eps) data[y * width + x] = 255;
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!IsBaked) return;
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3(
            WorldOrigin.x + WorldSize.x * 0.5f, transform.position.y,
            WorldOrigin.y + WorldSize.y * 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(WorldSize.x, 0.1f, WorldSize.y));
    }
}