using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps an R8 "explored" texture with the same size/bounds as the walkable mask from
/// MinimapMaskBaker, and paints a soft disc around the target (player) as it moves.
/// Published as shader global:
/// _MinimapExploredMask: Texture2D (0 = unexplored, 255 = explored)
/// Use the same UV as the walkable mask (see _MinimapWorldRect).
/// </summary>
public class MinimapExploredMask : MonoBehaviour
{
    private static readonly int ExploredMaskId = Shader.PropertyToID("_MinimapExploredMask");
    
    [Header("References")]
    [SerializeField] private MinimapMaskBaker baker;
    [Tooltip("Usually the player transform.")]
    [SerializeField] private Transform target;
    
    [Header("Reveal Settings")]
    [Tooltip("Radius (meters) revealed around the target.")]
    [SerializeField, Min(0.5f)] private float revealRadius = 8f;
    [Tooltip("Width (meters) of the soft falloff at the edge of the disc.")]
    [SerializeField, Min(0f)] private float softness = 2f;
    [Tooltip("Target must move at least this far (meters) before painting again.")]
    [SerializeField, Min(0.1f)] private float updateDistance = 1f;
    
    [Header("Debug (optional)")]
    [SerializeField] private RawImage debugPreview;

    private Texture2D exploredMask;
    private byte[] data;
    private int width;
    private int height;
    private Vector3 lastPos;
    private bool hasLast;
    
    public Texture2D ExploredMask => exploredMask;
    public bool IsReady => exploredMask != null;
    
    private void OnEnable()
    {
        if(baker == null) return;
        baker.Baked += Init;
        if(baker.IsBaked) Init();
    }

    private void OnDisable()
    {
        if(baker != null) baker.Baked -= Init;
    }

    private void OnDestroy()
    {
        if(exploredMask != null) SafeDestroy(exploredMask);
    }

    private void Init()
    {
        Texture2D walkable = baker.WalkableMask;
        if(walkable == null) return;
        
        width = walkable.width;
        height = walkable.height;
        
        if(exploredMask != null) SafeDestroy(exploredMask);

        exploredMask = new Texture2D(width, height, TextureFormat.R8, false, true)
        {
            name = "MinimapExploredMask",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        data = new byte[width * height];
        Upload();
        
        Shader.SetGlobalTexture(ExploredMaskId, exploredMask);
        if(debugPreview != null) debugPreview.texture = exploredMask;

        hasLast = false;
        if(target != null) Reveal(target.position);
    }

    private void Update()
    {
        if (!IsReady || target == null) return;
 
        Vector3 p = target.position;
        if (hasLast)
        {
            float dx = p.x - lastPos.x;
            float dz = p.z - lastPos.z;
            if (dx * dx + dz * dz < updateDistance * updateDistance) return;
        }
 
        Reveal(p);
    }

    /// <summary>
    /// Paint a soft disc at a world position (keeps the max with existing values).
    /// </summary>
    public void Reveal(Vector3 worldPos)
    {
        if(!IsReady) return;

        float mpt = baker.MetersPerTexel;
        float cx = (worldPos.x - baker.WorldOrigin.x) / mpt; // continuous texel coords
        float cy = (worldPos.z - baker.WorldOrigin.y) / mpt;

        float rTexels = revealRadius / mpt;
        float softTexels = Mathf.Max(softness / mpt, 0.0001f);
        int r = Mathf.CeilToInt(rTexels);

        int minX = Mathf.Max(0, Mathf.FloorToInt(cx) - r);
        int maxX = Mathf.Min(width - 1, Mathf.FloorToInt(cx) + r);
        int minY = Mathf.Max(0, Mathf.FloorToInt(cy) - r);
        int maxY = Mathf.Min(height - 1, Mathf.FloorToInt(cy) + r);

        bool changed = false;

        for (int y = minY; y <= maxY; y++)
        {
            float dy = y + 0.5f - cy;
            
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x + 0.5f - cx;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float v = Mathf.Clamp01((rTexels - dist) / softTexels);
                byte b = (byte)(v * 255f);
 
                int i = y * width + x;
                if (b > data[i])
                {
                    data[i] = b;
                    changed = true;
                }
            }
        }
        
        lastPos = worldPos;
        hasLast = true;
        
        if(changed) Upload();
    }

    // ---------- Utilities ----------
    
    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        if(!IsReady) return;
        System.Array.Clear(data, 0, data.Length);
        Upload();
        hasLast = false;
    }

    [ContextMenu("Reveal All (Debug)")]
    public void RevealAll()
    {
        if(!IsReady) return;
        for (int i = 0; i < data.Length; i++) data[i] = 255;
        Upload();
    }

    /// <summary>
    /// Copy of the explored data, for the save system.
    /// </summary>
    public byte[] GetSaveData()
    {
        return IsReady ? (byte[])data.Clone() : null;
    }

    /// <summary>
    /// Restore explored data. Must be the same size as the current mask.
    /// </summary>
    public bool LoadSaveData(byte[] saved)
    {
        if (!IsReady || saved == null || saved.Length != data.Length) return false;
        System.Buffer.BlockCopy(saved, 0, data, 0, saved.Length);
        Upload();
        return true;
    }
    
    private void Upload()
    {
        exploredMask.SetPixelData(data, 0);
        exploredMask.Apply(false, false);
    }
    
    private static void SafeDestroy(UnityEngine.Object obj)
    {
        if(Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}