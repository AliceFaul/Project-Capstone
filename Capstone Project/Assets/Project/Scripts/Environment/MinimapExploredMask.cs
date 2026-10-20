using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fog of war for the minimap. Keeps the "visible map" the overlay shader draws.
///
/// Everything lives on ONE coarse grid (1 cell = cellFactor texels of the baker mask, e.g. 3 x 0.1 = 0.3 m):
///   - walkCells : which cells are walkable (built once from the baker's walkable mask)
///   - visible   : which cells the player already revealed (this is also the texture the shader reads)
///
/// Reveal = flood fill (4-neighbor) over walkable cells, starting at the player and limited by
/// revealRadius, so the revealed shape follows corridors / rooms and never leaks through non-walkable areas.
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
    [SerializeField, Min(0.5f)] private float revealRadius = 14f;
 
    [Tooltip("Target must move at least this far (meters) before revealing again.")]
    [SerializeField, Min(0.1f)] private float updateDistance = 0.5f;
 
    [Tooltip("The target may stand up to this far (meters) outside the walkable area and still reveal.")]
    [SerializeField, Min(0.1f)] private float snapDistance = 1.5f;
 
    [Header("Map Look")]
    [Tooltip("Map cell size = this x metersPerTexel of the baker (3 x 0.1 = 0.3 m). Bigger = chunkier.")]
    [SerializeField, Range(1, 8)] private int cellFactor = 3;
 
    [Tooltip("Fraction of a cell that must be walkable for the cell to count as map. " +
             "1 = only cells fully inside the NavMesh (outline never sticks out of the NavMesh).")]
    [SerializeField, Range(0.5f, 1f)] private float cellCoverage = 1f;
 
    [Header("Debug (optional)")]
    [SerializeField] private RawImage debugPreview;
 
    private byte[] walkCells;   // 1 = walkable cell
    private byte[] visible;     // 255 = revealed cell (uploaded as is)
    private int cellsW;
    private int cellsH;
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
 
        int width = walkable.width;
        int height = walkable.height;
        var fine = walkable.GetRawTextureData<byte>();
 
        cellsW = Mathf.CeilToInt(width / (float)cellFactor);
        cellsH = Mathf.CeilToInt(height / (float)cellFactor);
        cellSize = cellFactor * baker.MetersPerTexel;
 
        walkCells = new byte[cellsW * cellsH];
        visible = new byte[cellsW * cellsH];
 
        // a cell is walkable when enough of its texels are walkable
        for (int cy = 0; cy < cellsH; cy++)
        {
            for (int cx = 0; cx < cellsW; cx++)
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
                        if (fine[row + fx] != 0) count++;
                    }
                }
 
                walkCells[cy * cellsW + cx] = count >= Mathf.CeilToInt(total * cellCoverage) ? (byte)1 : (byte)0;
            }
        }
 
        if (visibleMask != null) Destroy(visibleMask);
        visibleMask = new Texture2D(cellsW, cellsH, TextureFormat.R8, false, true)
        {
            name = "MinimapVisibleMask",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        UploadVisible();
 
        Shader.SetGlobalTexture(VisibleMaskId, visibleMask);
        Shader.SetGlobalVector(VisibleInfoId, new Vector4(cellSize, cellsW, cellsH, 0f));
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
 
        // remember the attempt even if it fails, so we don't retry every frame while standing off the NavMesh
        lastPos = worldPos;
        hasLast = true;
 
        Vector2Int start = WorldToCell(worldPos);
        int snapR = Mathf.Max(1, Mathf.CeilToInt(snapDistance / cellSize));
        if (!FindWalkableNear(ref start, snapR)) return;
 
        int rC = Mathf.Max(1, Mathf.CeilToInt(revealRadius / cellSize));
        int win = 2 * rC + 1;
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
 
        int wx0 = start.x - rC;
        int wy0 = start.y - rC;
        int r2 = rC * rC;
 
        int head = 0, tail = 0;
        int startLocal = rC * win + rC;
        seen[startLocal] = true;
        queue[tail++] = startLocal;
 
        bool changed = false;
 
        while (head < tail)
        {
            int li = queue[head++];
            int lx = li % win;
            int ly = li / win;
 
            int gi = (wy0 + ly) * cellsW + (wx0 + lx);
            if (visible[gi] == 0)
            {
                visible[gi] = 255;
                changed = true;
            }
 
            for (int k = 0; k < 4; k++)
            {
                int nlx = lx + DX[k];
                int nly = ly + DY[k];
                if (nlx < 0 || nly < 0 || nlx >= win || nly >= win) continue;
 
                int ddx = nlx - rC;
                int ddy = nly - rC;
                if (ddx * ddx + ddy * ddy > r2) continue;
 
                int ngx = wx0 + nlx;
                int ngy = wy0 + nly;
                if (ngx < 0 || ngy < 0 || ngx >= cellsW || ngy >= cellsH) continue;
 
                int ni = nly * win + nlx;
                if (seen[ni]) continue;
                if (walkCells[ngy * cellsW + ngx] == 0) continue;
 
                seen[ni] = true;
                queue[tail++] = ni;
            }
        }
 
        if (changed) UploadVisible();
    }
 
    // ---------- Internals ----------
 
    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt((worldPos.x - baker.WorldOrigin.x) / cellSize),
            Mathf.FloorToInt((worldPos.z - baker.WorldOrigin.y) / cellSize));
    }
 
    private bool FindWalkableNear(ref Vector2Int cell, int maxR)
    {
        if (cell is { x: >= 0, y: >= 0 } && cell.x < cellsW && cell.y < cellsH &&
            walkCells[cell.y * cellsW + cell.x] != 0)
            return true;
 
        int best = int.MaxValue;
        Vector2Int bestCell = cell;
 
        for (int dy = -maxR; dy <= maxR; dy++)
        {
            for (int dx = -maxR; dx <= maxR; dx++)
            {
                int x = cell.x + dx;
                int y = cell.y + dy;
                if (x < 0 || y < 0 || x >= cellsW || y >= cellsH) continue;
                if (walkCells[y * cellsW + x] == 0) continue;
 
                int d2 = dx * dx + dy * dy;
                if (d2 < best)
                {
                    best = d2;
                    bestCell = new Vector2Int(x, y);
                }
            }
        }
 
        if (best == int.MaxValue) return false;
        cell = bestCell;
        return true;
    }
 
    private void UploadVisible()
    {
        visibleMask.SetPixelData(visible, 0);
        visibleMask.Apply(false, false);
    }
 
    // ---------- Utilities ----------
 
    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        if (!IsReady) return;
        System.Array.Clear(visible, 0, visible.Length);
        UploadVisible();
        hasLast = false;
    }
 
    [ContextMenu("Reveal All (debug)")]
    public void RevealAll()
    {
        if (!IsReady) return;
        for (int i = 0; i < visible.Length; i++)
            visible[i] = walkCells[i] != 0 ? (byte)255 : (byte)0;
        UploadVisible();
    }
 
    /// <summary>
    /// Copy of the revealed cells, for the save system (coarse grid, ~9x smaller than before).
    /// </summary>
    public byte[] GetSaveData()
    {
        return IsReady ? (byte[])visible.Clone() : null;
    }
 
    /// <summary>
    /// Restore revealed cells. Must have the same size as the current cell grid
    /// (same NavMesh, same baker settings, same cellFactor).
    /// </summary>
    public bool LoadSaveData(byte[] saved)
    {
        if (!IsReady || saved == null || saved.Length != visible.Length) return false;
        System.Buffer.BlockCopy(saved, 0, visible, 0, saved.Length);
        UploadVisible();
        return true;
    }
}