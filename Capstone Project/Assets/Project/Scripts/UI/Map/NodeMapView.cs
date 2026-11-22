using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NodeMapView : MonoBehaviour
{
    public static NodeMapView Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance._open;
    // Frame in which this screen used Esc, so the pause menu does not react to the same key press.
    public static int EscapeConsumedFrame { get; private set; } = -1;
    public static event Action Closed;

    [SerializeField] private GameObject screenRoot;
    [SerializeField] private RectTransform mapArea;
    [SerializeField] private RectTransform edgeLayer;
    [SerializeField] private RectTransform nodeLayer;
    [SerializeField] private NodeMapNodeView nodePrefab;
    [SerializeField] private TMP_Text columnLabelPrefab;
    [SerializeField] private NodeMapStyle style;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text progressText;

    [Header("Selected node")]
    [SerializeField] private Image infoDot;
    [SerializeField] private TMP_Text infoName;
    [SerializeField] private GameObject infoTagRoot;
    [SerializeField] private Image infoTagBackground;
    [SerializeField] private TMP_Text infoTagText;
    [SerializeField] private TMP_Text infoDescription;
    [SerializeField] private GameObject rowHealth;
    [SerializeField] private TMP_Text healthValue;
    [SerializeField] private GameObject rowDamage;
    [SerializeField] private TMP_Text damageValue;
    [SerializeField] private GameObject rowLoot;
    [SerializeField] private TMP_Text lootValue;

    private static readonly Color PathColor = new Color(0.94f, 0.50f, 0.16f);
    private static readonly Color NextColor = Color.white;
    private static readonly Color OffColor = new Color(0.17f, 0.29f, 0.36f);

    private readonly List<NodeMapNodeView> _views = new List<NodeMapNodeView>();
    private InputHandler _input;
    private InventoryScreenController _inventory;
    private LevelSelectView _levelSelect;
    private ShopSceneDirector[] _shops;
    private PauseMenuController _pause;
    private NodeMapNodeView _selected;
    private bool _open;
    private bool _ownsPause;
    private float _previousTimeScale = 1f;
    private float _nextHookCheck;

    private static string T(string key, params object[] args) => InventoryText.Get(key, args);

    private void Awake()
    {
        Instance = this;
        screenRoot.SetActive(false);
    }

    private void Start()
    {
        _inventory = FindFirstObjectByType<InventoryScreenController>(FindObjectsInactive.Include);
        _levelSelect = FindFirstObjectByType<LevelSelectView>(FindObjectsInactive.Include);
        _shops = FindObjectsByType<ShopSceneDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _pause = FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
    }

    private void OnDestroy()
    {
        //if (_input != null) _input.OnToggleNodeMap -= Toggle;
        if (_open) Restore();
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // the player (and its InputHandler) may appear after this screen: look for it every half second
        if (_input == null && Time.unscaledTime >= _nextHookCheck)
        {
            _nextHookCheck = Time.unscaledTime + 0.5f;
            _input = FindFirstObjectByType<InputHandler>();
            //if (_input != null) _input.OnToggleNodeMap += Toggle;
        }

        if (!_open) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            EscapeConsumedFrame = Time.frameCount;
            Close();
        }
    }

    private void Toggle()
    {
        if (_open) Close();
        else if (!OtherScreenOpen()) Open();
    }

    private bool OtherScreenOpen()
    {
        if (_inventory != null && _inventory.IsOpen) return true;
        if (_levelSelect != null && _levelSelect.IsOpen) return true;
        if (_pause != null && _pause.IsOpen) return true;
        if (SettingsScreen.IsOpen) return true;

        if (_shops != null)
        {
            foreach (var shop in _shops)
            {
                if (shop != null && shop.IsOpen) return true;
            }
        }
        return false;
    }

    public void Open()
    {
        var run = LevelRunController.Instance;
        if (_open || run == null || !run.IsRunning || run.IsTransitioning || run.Graph == null || run.Current == null) return;

        _open = true;
        _ownsPause = Time.timeScale > 0.01f; // opened from the pause menu: the game is already paused
        if (_ownsPause)
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            LockInput(true);
            AudioService.Instance?.SetOverlay(this, true);
        }

        screenRoot.SetActive(true);
        Build(run);
    }

    public void Close()
    {
        if (!_open) return;

        _open = false;
        screenRoot.SetActive(false);
        Restore();
        Closed?.Invoke();
    }

    private void Restore()
    {
        if (!_ownsPause) return;

        _ownsPause = false;
        Time.timeScale = _previousTimeScale <= 0.01f ? 1f : _previousTimeScale;
        LockInput(false);
        AudioService.Instance?.SetOverlay(this, false);
    }

    private static void LockInput(bool locked)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        player.CmdCombatLocked(locked);
        var artifacts = player.ArtifactController;
        if (artifacts != null) artifacts.InputLocked = locked;
    }

    private void Build(LevelRunController run)
    {
        Clear();

        var graph = run.Graph;
        var current = run.Current;

        // header
        var level = run.Level;
        var settings = level != null ? level.GetDifficulty(run.DifficultyIndex) : null;
        string levelName = level != null ? level.GetName() : "-";
        string difficulty = settings != null ? T("DIFFICULTY_" + settings.difficulty.ToString().ToUpper()) : "-";
        subtitleText.text = $"{levelName} - {difficulty}".ToUpperInvariant();
        progressText.text = T("NODEMAP_NODE_FMT", current.Layer + 1, graph.LayerCount);

        // positions: one column per layer, nodes of a layer spread vertically around the middle
        Vector2 size = mapArea.rect.size;
        const float margin = 90f;
        int widest = 1;
        for (int layer = 0; layer < graph.LayerCount; layer++) widest = Mathf.Max(widest, graph.GetLayer(layer).Count);
        float rowStep = widest > 1 ? Mathf.Min(200f, (size.y - 2f * margin) / (widest - 1)) : 0f;

        var positions = new Dictionary<int, Vector2>();
        for (int layer = 0; layer < graph.LayerCount; layer++)
        {
            var nodes = graph.GetLayer(layer);
            float x = graph.LayerCount > 1 ? margin + layer * (size.x - 2f * margin) / (graph.LayerCount - 1) : size.x * 0.5f;
            for (int i = 0; i < nodes.Count; i++)
            {
                float yFromTop = size.y * 0.5f + (i - (nodes.Count - 1) * 0.5f) * rowStep;
                positions[nodes[i].Id] = new Vector2(x, -yFromTop);
            }
            AddColumnLabel(layer, graph.LayerCount, x);
        }

        // edges: faint first, then the walked path, then the ones you can take from here
        var path = new List<(Vector2, Vector2)>();
        var next = new List<(Vector2, Vector2)>();
        foreach (var node in graph.Nodes)
        {
            foreach (int nextId in node.Next)
            {
                var edge = (positions[node.Id], positions[nextId]);
                if (node.Visited && graph.Get(nextId).Visited) path.Add(edge);
                else if (node == current) next.Add(edge);
                else Line(edge.Item1, edge.Item2, OffColor, 3f, false);
            }
        }
        foreach (var edge in path) Line(edge.Item1, edge.Item2, PathColor, 5f, false);
        foreach (var edge in next) Line(edge.Item1, edge.Item2, NextColor, 4f, true);

        // nodes
        NodeMapNodeView firstNext = null;
        foreach (var node in graph.Nodes)
        {
            var state = node == current ? NodeMapNodeView.State.Current
                : current.Next.Contains(node.Id) ? NodeMapNodeView.State.Next
                : node.Visited ? NodeMapNodeView.State.Visited
                : NodeMapNodeView.State.Off;

            var view = Instantiate(nodePrefab, nodeLayer);
            var rect = (RectTransform)view.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = positions[node.Id];
            view.Setup(node, state, style.Get(node.Type), node.Type == NodeType.Boss ? 104f : 72f, T("NODEMAP_TAG_HERE"));
            view.Highlighted += OnNodeHighlighted;
            _views.Add(view);

            if (state == NodeMapNodeView.State.Next && firstNext == null) firstNext = view;
        }

        var start = firstNext != null ? firstNext : _views.Find(v => v.Node == current);
        if (start != null)
        {
            OnNodeHighlighted(start);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(start.gameObject);
        }
    }

    private void Clear()
    {
        foreach (var view in _views)
        {
            if (view != null) Destroy(view.gameObject);
        }
        _views.Clear();
        _selected = null;

        foreach (Transform child in edgeLayer) Destroy(child.gameObject);
    }

    private void AddColumnLabel(int layer, int layerCount, float x)
    {
        var label = Instantiate(columnLabelPrefab, edgeLayer);
        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, 0f);
        label.text = layer == 0 ? T("NODEMAP_START") : layer == layerCount - 1 ? T("NODEMAP_BOSS") : (layer + 1).ToString();
    }

    private void Line(Vector2 from, Vector2 to, Color color, float thickness, bool dashed)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        Vector2 direction = delta / Mathf.Max(0.0001f, length);

        if (!dashed)
        {
            Segment(from, direction, length, color, thickness);
            return;
        }
        for (float t = 0f; t < length; t += 14f) Segment(from + direction * t, direction, Mathf.Min(4f, length - t), color, thickness);
    }

    private void Segment(Vector2 start, Vector2 direction, float length, Color color, float thickness)
    {
        var go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(edgeLayer, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = start;
        rect.sizeDelta = new Vector2(length, thickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private void OnNodeHighlighted(NodeMapNodeView view)
    {
        if (_selected != null) _selected.SetSelected(false);
        _selected = view;
        view.SetSelected(true);
        ShowInfo(view);
    }

    private void ShowInfo(NodeMapNodeView view)
    {
        var node = view.Node;
        var entry = style.Get(node.Type);
        string typeKey = node.Type.ToString().ToUpperInvariant();

        infoDot.color = entry.color;
        infoName.text = T("NODE_" + typeKey);
        infoDescription.text = T("NODE_DESC_" + typeKey);

        string tag = view.CurrentState == NodeMapNodeView.State.Current ? T("NODEMAP_TAG_HERE")
            : view.CurrentState == NodeMapNodeView.State.Next ? T("NODEMAP_TAG_NEXT")
            : view.CurrentState == NodeMapNodeView.State.Visited ? T("NODEMAP_TAG_VISITED")
            : null;
        infoTagRoot.SetActive(tag != null);
        if (tag != null)
        {
            infoTagText.text = tag;
            infoTagBackground.color = view.CurrentState == NodeMapNodeView.State.Visited ? PathColor : Color.white;
        }

        bool fight = node.Type == NodeType.Combat || node.Type == NodeType.Elite || node.Type == NodeType.Boss;
        bool showEnemy = fight && view.CurrentState != NodeMapNodeView.State.Visited;
        rowHealth.SetActive(showEnemy);
        rowDamage.SetActive(showEnemy);
        rowLoot.SetActive(node.Type == NodeType.Elite);

        var run = LevelRunController.Instance;
        if (showEnemy && run != null)
        {
            run.PreviewEnemyScaling(node, out float health, out float damage, out bool exact);
            string prefix = exact ? "x" : "~x"; // ~ = depends on the nodes in between
            healthValue.text = prefix + health.ToString("0.00");
            damageValue.text = prefix + damage.ToString("0.00");
        }
        lootValue.text = T("NODEMAP_LOOT_BETTER");
    }
}