using UnityEngine;
using UnityEngine.UI;

/// <summary>
///   Keeps the "visible map" the overlay shader draws.
///
/// - Reveal = flood fill over the walkable mask (4-neighbor, limited by revealRadius), starting at the
///   player. The revealed shape therefore follows corridors / rooms / holes and never leaks through
///   non-walkable areas (no more disc that sees through walls).
/// - The result is stored in a COARSE R8 texture (one texel = one map cell = cellFactor fine texels),
///   so the shader needs only a few point loads and gets the stepped Minecraft-Dungeons look for free.
///
/// Shader globals:
///   _MinimapVisibleMask : Texture2D R8, 255 = visible map cell
///   _MinimapVisibleInfo : (cellSizeMeters, width, height, 0)
/// The cell grid starts at the same origin as the walkable mask (_MinimapWorldRect.xy).
/// </summary>
public class MinimapExploredMask : MonoBehaviour
{
    private static readonly int VisibleMaskId = Shader.PropertyToID("_MinimapVisibleMask");
    private static readonly int VisibleInfoId = Shader.PropertyToID("_MinimapVisibleInfo");
 
    private static readonly int[] DX = { -1, 1, 0, 0 };
    private static readonly int[] DY = { 0, 0, -1, 1 };
 
    [Header("References")]
    [SerializeField] private MinimapMaskBaker baker;
    [Tooltip("Usually the Player transform.")]
    [SerializeField] private Transform target;
 
    [Header("Reveal Settings")]
    [Tooltip("Max distance (meters) revealed around the target, following walkable space.")]
    [SerializeField, Min(0.5f)] private float revealRadius = 5f;
 
    [Tooltip("Target must move at least this far (meters) before revealing again.")]
    [SerializeField, Min(0.1f)] private float updateDistance = 0.5f;
 
    [Header("Map Look")]
    [Tooltip("Map cell size = this x metersPerTexel of the baker (3 x 0.1 = 0.3 m). Bigger = chunkier.")]
    [SerializeField, Range(1, 8)] private int cellFactor = 3;
 
    [Header("Debug (optional)")]
    [SerializeField] private RawImage debugPreview;
 
    // fine data (same size as the walkable mask)
    private byte[] walk;
    private byte[] explored;
    private int width;
    private int height;
 
    // coarse data (what the shader reads)
    private byte[] coarse;
    private int coarseW;
    private int coarseH;
    private float cellSize;
    private Texture2D visibleMask;
 
    // flood fill buffers
    private bool[] seen;
    private int[] queue;
 
    private Vector3 lastPos;
    private bool hasLast;
 
    public Texture2D VisibleMask => visibleMask;
    public bool IsReady => visibleMask != null;
 
    private void OnEnable()
    {
        if (baker == null) return;
        baker.Baked += Init;
        if (baker.IsBaked) Init();
    }
 
    private void OnDisable()
    {
        if (baker != null) baker.Baked -= Init;
    }
 
    private void OnDestroy()
    {
        if (visibleMask != null) Destroy(visibleMask);
    }
 
    private void Init()
    {
        Texture2D walkable = baker.WalkableMask;
        if (walkable == null) return;
 
        width = walkable.width;
        height = walkable.height;
 
        walk = walkable.GetRawTextureData<byte>().ToArray();
        explored = new byte[width * height];
 
        coarseW = Mathf.CeilToInt(width / (float)cellFactor);
        coarseH = Mathf.CeilToInt(height / (float)cellFactor);
        coarse = new byte[coarseW * coarseH];
        cellSize = cellFactor * baker.MetersPerTexel;
 
        if (visibleMask != null) Destroy(visibleMask);
        visibleMask = new Texture2D(coarseW, coarseH, TextureFormat.R8, false, true)
        {
            name = "MinimapVisibleMask",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        UploadCoarse();
 
        Shader.SetGlobalTexture(VisibleMaskId, visibleMask);
        Shader.SetGlobalVector(VisibleInfoId, new Vector4(cellSize, coarseW, coarseH, 0f));
        if (debugPreview != null) debugPreview.texture = visibleMask;
 
        seen = null;
        queue = null;
        hasLast = false;
 
        if (target != null) Reveal(target.position);
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
    /// Reveal the walkable space reachable from a world position (within revealRadius).
    /// </summary>
    public void Reveal(Vector3 worldPos)
    {
        if (!IsReady) return;
 
        Vector2Int start = baker.WorldToTexel(worldPos);
        if (!FindWalkableNear(ref start, 6)) return;
 
        lastPos = worldPos;
        hasLast = true;
 
        int rT = Mathf.Max(1, Mathf.CeilToInt(revealRadius / baker.MetersPerTexel));
        int win = 2 * rT + 1;
        int winCount = win * win;
 
        if (seen == null || seen.Length != winCount)
        {
            seen = new bool[winCount];
            queue = new int[winCount];
        }
        else
        {
            System.Array.Clear(seen, 0, winCount);
        }
 
        int wx0 = start.x - rT;
        int wy0 = start.y - rT;
        int r2 = rT * rT;
 
        int head = 0, tail = 0;
        int startLocal = rT * win + rT;
        seen[startLocal] = true;
        queue[tail++] = startLocal;
 
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
 
        while (head < tail)
        {
            int li = queue[head++];
            int lx = li % win;
            int ly = li / win;
            int gx = wx0 + lx;
            int gy = wy0 + ly;
 
            explored[gy * width + gx] = 255;
            if (gx < minX) minX = gx;
            if (gx > maxX) maxX = gx;
            if (gy < minY) minY = gy;
            if (gy > maxY) maxY = gy;
 
            for (int k = 0; k < 4; k++)
            {
                int nlx = lx + DX[k];
                int nly = ly + DY[k];
                if (nlx < 0 || nly < 0 || nlx >= win || nly >= win) continue;
 
                int ddx = nlx - rT;
                int ddy = nly - rT;
                if (ddx * ddx + ddy * ddy > r2) continue;
 
                int ngx = wx0 + nlx;
                int ngy = wy0 + nly;
                if (ngx < 0 || ngy < 0 || ngx >= width || ngy >= height) continue;
 
                int ni = nly * win + nlx;
                if (seen[ni]) continue;
                if (walk[ngy * width + ngx] == 0) continue;
 
                seen[ni] = true;
                queue[tail++] = ni;
            }
        }
 
        UpdateCoarse(minX, minY, maxX, maxY);
        UploadCoarse();
    }
 
    // ---------- Coarse map ----------
 
    private void UpdateCoarse(int fineMinX, int fineMinY, int fineMaxX, int fineMaxY)
    {
        int cx0 = Mathf.Clamp(fineMinX / cellFactor, 0, coarseW - 1);
        int cx1 = Mathf.Clamp(fineMaxX / cellFactor, 0, coarseW - 1);
        int cy0 = Mathf.Clamp(fineMinY / cellFactor, 0, coarseH - 1);
        int cy1 = Mathf.Clamp(fineMaxY / cellFactor, 0, coarseH - 1);
 
        for (int cy = cy0; cy <= cy1; cy++)
        {
            for (int cx = cx0; cx <= cx1; cx++)
            {
                int fx0 = cx * cellFactor;
                int fy0 = cy * cellFactor;
                int fx1 = Mathf.Min(fx0 + cellFactor, width);
                int fy1 = Mathf.Min(fy0 + cellFactor, height);
 
                int total = (fx1 - fx0) * (fy1 - fy0);
                int count = 0;
                for (int fy = fy0; fy < fy1; fy++)
                {
                    int row = fy * width;
                    for (int fx = fx0; fx < fx1; fx++)
                    {
                        if (explored[row + fx] != 0) count++;
                    }
                }
 
                // a cell is part of the map when at least half of it is explored walkable space
                coarse[cy * coarseW + cx] = (count * 2 >= total) ? (byte)255 : (byte)0;
            }
        }
    }
 
    private void UploadCoarse()
    {
        visibleMask.SetPixelData(coarse, 0);
        visibleMask.Apply(false, false);
    }
 
    private bool FindWalkableNear(ref Vector2Int texel, int maxR)
    {
        if (texel.x >= 0 && texel.y >= 0 && texel.x < width && texel.y < height &&
            walk[texel.y * width + texel.x] != 0)
            return true;
 
        int best = int.MaxValue;
        Vector2Int bestTexel = texel;
 
        for (int dy = -maxR; dy <= maxR; dy++)
        {
            for (int dx = -maxR; dx <= maxR; dx++)
            {
                int x = texel.x + dx;
                int y = texel.y + dy;
                if (x < 0 || y < 0 || x >= width || y >= height) continue;
                if (walk[y * width + x] == 0) continue;
 
                int d2 = dx * dx + dy * dy;
                if (d2 < best)
                {
                    best = d2;
                    bestTexel = new Vector2Int(x, y);
                }
            }
        }
 
        if (best == int.MaxValue) return false;
        texel = bestTexel;
        return true;
    }
 
    // ---------- Utilities ----------
 
    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        if (!IsReady) return;
        System.Array.Clear(explored, 0, explored.Length);
        System.Array.Clear(coarse, 0, coarse.Length);
        UploadCoarse();
        hasLast = false;
    }
 
    [ContextMenu("Reveal All (debug)")]
    public void RevealAll()
    {
        if (!IsReady) return;
        System.Buffer.BlockCopy(walk, 0, explored, 0, walk.Length);
        RebuildCoarse();
    }
 
    /// <summary>
    /// Copy of the explored data (fine resolution), for the save system.
    /// </summary>
    public byte[] GetSaveData()
    {
        return IsReady ? (byte[])explored.Clone() : null;
    }
 
    /// <summary>
    /// Restore explored data. Must have the same size as the current walkable mask.
    /// </summary>
    public bool LoadSaveData(byte[] saved)
    {
        if (!IsReady || saved == null || saved.Length != explored.Length) return false;
        System.Buffer.BlockCopy(saved, 0, explored, 0, saved.Length);
        RebuildCoarse();
        return true;
    }
 
    private void RebuildCoarse()
    {
        UpdateCoarse(0, 0, width - 1, height - 1);
        UploadCoarse();
    }
}