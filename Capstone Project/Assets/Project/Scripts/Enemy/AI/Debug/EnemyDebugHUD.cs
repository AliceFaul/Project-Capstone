using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mhieu.Enemy
{
    using Mhieu.AI.BT;

    /// <summary>
    /// Công cụ debug AI dùng chung cho MỌI enemy. KHÔNG cần đặt gì vào scene hay prefab:
    /// khi bấm Play (trong Editor hoặc Development Build) nó tự tạo một object "[EnemyDebugHUD]".
    ///
    /// Hiển thị:
    ///  - Trên đầu mỗi enemy (Game view): tên, state, chuỗi node đang chạy, khoảng cách tới player, cờ aggro...
    ///  - Bảng chi tiết cả cây hành vi (node đang chạy tô xanh) của enemy đang CHỌN trong Hierarchy
    ///    (không chọn enemy nào thì lấy enemy gần player nhất).
    ///  - Nhật ký sự kiện: đổi state, đổi nhánh hành vi, bắt đầu/mất aggro, về nhà, đổi pha boss, lấy/trả thẻ tấn công.
    ///  - Gizmo trên mặt đất cho mọi enemy: tầm phát hiện, tầm bỏ đuổi, leash, tầm chiêu, hitbox cận chiến.
    ///    (cần bật nút Gizmos của Game view / Scene view)
    ///
    /// Phím (Game view cần đang được focus):
    ///   F3  bật/tắt toàn bộ HUD
    ///   F4  bật/tắt ghi log ra Console
    ///   F5  in cây hành vi + blackboard của enemy đang chọn ra Console (tiện để copy gửi người khác)
    ///
    /// Muốn chỉnh các công tắc nhỏ khi đang Play: chọn object "[EnemyDebugHUD]" trong Hierarchy.
    /// Muốn tắt hẳn: đổi AutoCreate bên dưới thành false hoặc xóa file này.
    ///
    /// Ghi chú kỹ thuật: để không phải sửa EnemyBrain, HUD đọc hai field private của nó ("_tree", "config")
    /// bằng reflection. Nếu sau này bạn đổi tên hai field đó, cập nhật hằng TreeFieldName / ConfigFieldName;
    /// khi không đọc được thì phần cây node và gizmo tầm tự ẩn đi, phần còn lại vẫn chạy.
    /// Chữ trên màn hình dùng tiếng Anh không dấu vì font mặc định của IMGUI không có đủ dấu tiếng Việt.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyDebugHUD : MonoBehaviour
    {
        private static readonly bool AutoCreate = true;

        private const string TreeFieldName = "_tree";
        private const string ConfigFieldName = "config";
        private const int MaxEvents = 14;

        private static readonly FieldInfo TreeField =
            typeof(EnemyBrain).GetField(TreeFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ConfigField =
            typeof(EnemyBrain).GetField(ConfigFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        private static EnemyDebugHUD _instance;

        [Header("Display (F3 toggles all)")]
        public bool visible = true;
        public bool showLabels = true;
        public bool showSelectedTree = true;
        public bool showEventLog = true;
        public bool showGizmos = true;

        [Header("Console (F4)")]
        public bool logToConsole;

        [Header("Tuning")]
        [Min(1f)] public float labelMaxDistance = 80f;
        [Min(0.05f)] public float pollInterval = 0.1f;

        private sealed class Probe
        {
            public EnemyBrain Brain;
            public IStateMachine State;
            public BossPhaseController Boss;
            public BehaviorTree Tree;
            public EnemyAIConfig Config;

            public CharacterStateType LastState;
            public string Path = "(none)";
            public bool LastAggro;
            public bool LastReturning;
            public bool LastToken;
            public int LastPhase = 1;
        }

        private readonly Dictionary<EnemyBrain, Probe> _probes = new Dictionary<EnemyBrain, Probe>();
        private readonly List<EnemyBrain> _toRemove = new List<EnemyBrain>();
        private readonly List<Transform> _holders = new List<Transform>();
        private readonly Queue<string> _events = new Queue<string>();
        private readonly StringBuilder _sb = new StringBuilder(512);

        private float _nextRefresh;
        private float _nextPoll;

        private GUIStyle _tagStyle;
        private GUIStyle _panelStyle;
        private Texture2D _background;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateInstance()
        {
            if (!AutoCreate || _instance != null) return;

            var go = new GameObject("[EnemyDebugHUD]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<EnemyDebugHUD>();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_background != null) Destroy(_background);
        }

        // ------------------------------------------------------------------ Update / polling

        private void Update()
        {
            ReadHotkeys(out bool toggleHud, out bool toggleConsole, out bool dumpSelected);

            if (toggleHud) visible = !visible;
            if (toggleConsole)
            {
                logToConsole = !logToConsole;
                Emit("console logging " + (logToConsole ? "ON" : "OFF"));
            }

            if (dumpSelected) DumpSelectedToConsole();

            if (!visible && !logToConsole) return;

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 0.5f;
                RefreshEnemies();
            }

            if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + pollInterval;
                AttackTokens.CopyHolders(_holders);

                foreach (Probe probe in _probes.Values) Poll(probe);
            }
        }

        private static void ReadHotkeys(out bool f3, out bool f4, out bool f5)
        {
            f3 = false;
            f4 = false;
            f5 = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            f3 = keyboard.f3Key.wasPressedThisFrame;
            f4 = keyboard.f4Key.wasPressedThisFrame;
            f5 = keyboard.f5Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            f3 = Input.GetKeyDown(KeyCode.F3);
            f4 = Input.GetKeyDown(KeyCode.F4);
            f5 = Input.GetKeyDown(KeyCode.F5);
#endif
        }

        private void RefreshEnemies()
        {
            EnemyBrain[] found = FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (!_probes.ContainsKey(found[i])) _probes[found[i]] = CreateProbe(found[i]);
            }

            _toRemove.Clear();
            foreach (KeyValuePair<EnemyBrain, Probe> pair in _probes)
            {
                if (pair.Key == null || !pair.Key.isActiveAndEnabled) _toRemove.Add(pair.Key);
            }

            for (int i = 0; i < _toRemove.Count; i++) _probes.Remove(_toRemove[i]);
        }

        private static Probe CreateProbe(EnemyBrain brain)
        {
            var probe = new Probe
            {
                Brain = brain,
                State = brain.GetComponent<IStateMachine>(),
                Boss = brain.GetComponent<BossPhaseController>(),
                Tree = TreeField != null ? TreeField.GetValue(brain) as BehaviorTree : null,
                Config = ConfigField != null ? ConfigField.GetValue(brain) as EnemyAIConfig : null
            };

            if (probe.State != null) probe.LastState = probe.State.CurrentState;
            if (probe.Boss != null) probe.LastPhase = probe.Boss.Phase;
            return probe;
        }

        private void Poll(Probe p)
        {
            EnemyBrain brain = p.Brain;
            if (brain == null) return;

            string who = brain.name;

            if (p.State != null)
            {
                CharacterStateType state = p.State.CurrentState;
                if (state != p.LastState)
                {
                    Emit($"{who}: state {p.LastState} -> {state}");
                    p.LastState = state;
                }
            }

            Blackboard bb = brain.Blackboard;
            if (bb != null)
            {
                if (bb.IsAggro != p.LastAggro)
                {
                    Emit($"{who}: aggro {(bb.IsAggro ? "ON" : "OFF")}");
                    p.LastAggro = bb.IsAggro;
                }

                if (bb.IsReturningHome != p.LastReturning)
                {
                    Emit($"{who}: {(bb.IsReturningHome ? "returning home" : "arrived home")}");
                    p.LastReturning = bb.IsReturningHome;
                }
            }

            if (p.Boss != null && p.Boss.Phase != p.LastPhase)
            {
                Emit($"{who}: boss phase {p.LastPhase} -> {p.Boss.Phase}");
                p.LastPhase = p.Boss.Phase;
            }

            bool hasToken = _holders.Contains(brain.transform);
            if (hasToken != p.LastToken)
            {
                Emit($"{who}: attack token {(hasToken ? "GAINED" : "released")}");
                p.LastToken = hasToken;
            }

            if (p.Tree != null)
            {
                _sb.Clear();
                TreeDump.AppendRunningPath(p.Tree.Root, _sb);
                string path = _sb.ToString();

                if (path != p.Path)
                {
                    Emit($"{who}: {p.Path}  =>  {path}");
                    p.Path = path;
                }
            }
        }

        private void Emit(string message)
        {
            string line = $"{Time.time:F1}s  {message}";

            _events.Enqueue(line);
            while (_events.Count > MaxEvents) _events.Dequeue();

            if (logToConsole) UnityEngine.Debug.Log("[AI] " + line);
        }

        // ------------------------------------------------------------------ IMGUI (Game view)

        private void OnGUI()
        {
            if (!visible) return;

            EnsureStyles();

            Probe focus = ResolveFocus();
            GUI.Label(new Rect(10f, 8f, 700f, 22f),
                $"AI DEBUG   [F3] hide   [F4] console log: {(logToConsole ? "ON" : "OFF")}   [F5] dump selected to console" +
                (focus != null ? $"   | focus: {focus.Brain.name}" : ""));

            if (showLabels) DrawLabels();
            if (showSelectedTree && focus != null) DrawTreePanel(focus);
            if (showEventLog) DrawEventLog();
        }

        private void DrawLabels()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            foreach (Probe p in _probes.Values)
            {
                if (p.Brain == null) continue;

                Vector3 world = p.Brain.transform.position + Vector3.up * 2.6f;
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0f || screen.z > labelMaxDistance) continue;

                var content = new GUIContent(BuildLabel(p));
                Vector2 size = _tagStyle.CalcSize(content);
                var rect = new Rect(screen.x - size.x * 0.5f, Screen.height - screen.y - size.y, size.x, size.y);
                GUI.Box(rect, content, _tagStyle);
            }
        }

        private string BuildLabel(Probe p)
        {
            Blackboard bb = p.Brain.Blackboard;

            var sb = new StringBuilder(128);
            sb.Append(p.Brain.name).Append("  [").Append(p.LastState).Append(']');
            if (p.LastToken) sb.Append("  TOKEN");

            sb.Append('\n').Append(p.Path);

            if (bb != null)
            {
                sb.Append('\n');
                sb.Append("d=").Append(float.IsInfinity(bb.DistToTarget) ? "-" : bb.DistToTarget.ToString("F1"));
                if (bb.IsAggro) sb.Append("  aggro");
                if (bb.IsReturningHome) sb.Append("  returning");
                if (bb.IsDisabled) sb.Append("  disabled");
                if (p.Boss != null) sb.Append("  phase ").Append(p.Boss.Phase);
            }

            return sb.ToString();
        }

        private void DrawTreePanel(Probe p)
        {
            _sb.Clear();
            Blackboard bb = p.Brain.Blackboard;

            _sb.Append("<b>").Append(p.Brain.name).Append("</b>  [").Append(p.LastState).Append("]\n");
            if (bb != null)
            {
                _sb.Append("target: ").Append(bb.HasTarget ? bb.Target.name : "none");
                _sb.Append("   dist: ").Append(float.IsInfinity(bb.DistToTarget) ? "-" : bb.DistToTarget.ToString("F1"));
                _sb.Append("   phase: ").Append(bb.Phase).Append('\n');
                _sb.Append("aggro: ").Append(bb.IsAggro);
                _sb.Append("   returning: ").Append(bb.IsReturningHome);
                _sb.Append("   disabled: ").Append(bb.IsDisabled).Append('\n');
            }

            _sb.Append("\nBehavior tree  (* = running)\n");
            if (p.Tree != null)
            {
                TreeDump.AppendTree(p.Tree.Root, _sb,
                    "<color=#7CFC7C>", "</color>", "<color=#8A8A8A>", "</color>");
            }
            else
            {
                _sb.Append("(tree not available)\n");
            }

            var content = new GUIContent(_sb.ToString());
            float height = _panelStyle.CalcHeight(content, 400f);
            GUI.Box(new Rect(10f, 34f, 400f, height), content, _panelStyle);
        }

        private void DrawEventLog()
        {
            if (_events.Count == 0) return;

            _sb.Clear();
            _sb.Append("<b>Events</b>\n");
            foreach (string line in _events) _sb.Append(line).Append('\n');

            var content = new GUIContent(_sb.ToString());
            float height = _panelStyle.CalcHeight(content, 560f);
            GUI.Box(new Rect(10f, Screen.height - height - 10f, 560f, height), content, _panelStyle);
        }

        private void EnsureStyles()
        {
            if (_tagStyle != null) return;

            _background = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
            _background.Apply();

            _tagStyle = new GUIStyle(GUI.skin.box)
            {
                richText = true,
                alignment = TextAnchor.UpperCenter,
                fontSize = 11,
                wordWrap = false,
                padding = new RectOffset(5, 5, 3, 3)
            };
            _tagStyle.normal.background = _background;
            _tagStyle.normal.textColor = Color.white;

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                richText = true,
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                wordWrap = false,
                padding = new RectOffset(8, 8, 6, 6)
            };
            _panelStyle.normal.background = _background;
            _panelStyle.normal.textColor = Color.white;
        }

        // ------------------------------------------------------------------ Focus (enemy được xem chi tiết)

        private Probe ResolveFocus()
        {
#if UNITY_EDITOR
            GameObject selected = UnityEditor.Selection.activeGameObject;
            if (selected != null)
            {
                EnemyBrain selectedBrain = selected.GetComponentInParent<EnemyBrain>();
                if (selectedBrain != null && _probes.TryGetValue(selectedBrain, out Probe selectedProbe))
                    return selectedProbe;
            }
#endif
            if (!PlayerRegistry.TryGetAlive(out Transform player)) return null;

            Probe nearest = null;
            float best = float.PositiveInfinity;
            foreach (Probe p in _probes.Values)
            {
                if (p.Brain == null) continue;

                float distance = AIMath.FlatDistance(p.Brain.transform.position, player.position);
                if (distance < best)
                {
                    best = distance;
                    nearest = p;
                }
            }

            return nearest;
        }

        private void DumpSelectedToConsole()
        {
            Probe p = ResolveFocus();
            if (p == null || p.Brain == null)
            {
                UnityEngine.Debug.Log("[AI] F5: không có enemy nào để dump.");
                return;
            }

            var sb = new StringBuilder(512);
            sb.Append("[AI] DUMP ").Append(p.Brain.name).Append("  time=").Append(Time.time.ToString("F2")).Append('\n');
            sb.Append("state: ").Append(p.LastState).Append('\n');

            Blackboard bb = p.Brain.Blackboard;
            if (bb != null)
            {
                sb.Append("target: ").Append(bb.HasTarget ? bb.Target.name : "none").Append('\n');
                sb.Append("distToTarget: ").Append(bb.DistToTarget.ToString("F2")).Append('\n');
                sb.Append("aggro: ").Append(bb.IsAggro).Append("  returning: ").Append(bb.IsReturningHome);
                sb.Append("  disabled: ").Append(bb.IsDisabled).Append("  phase: ").Append(bb.Phase).Append('\n');
                sb.Append("homeDist: ")
                    .Append(AIMath.FlatDistance(p.Brain.transform.position, bb.HomePosition).ToString("F2")).Append('\n');
            }

            sb.Append("attackToken: ").Append(p.LastToken)
                .Append("  (").Append(AttackTokens.HolderCount).Append('/').Append(AttackTokens.MaxMeleeAttackers).Append(")\n");

            sb.Append("running path: ").Append(p.Path).Append("\ntree:\n");
            if (p.Tree != null) TreeDump.AppendTree(p.Tree.Root, sb);

            UnityEngine.Debug.Log(sb.ToString());
        }

        // ------------------------------------------------------------------ Gizmos (Scene view và Game view có bật Gizmos)

        private void OnDrawGizmos()
        {
            if (!visible || !showGizmos || !Application.isPlaying) return;

            foreach (Probe p in _probes.Values)
            {
                if (p.Brain != null) DrawProbeGizmos(p);
            }
        }

        private static void DrawProbeGizmos(Probe p)
        {
            Transform t = p.Brain.transform;
            Blackboard bb = p.Brain.Blackboard;
            EnemyAIConfig cfg = p.Config;

            if (cfg != null)
            {
                bool aggro = bb != null && bb.IsAggro;

                Gizmos.color = aggro ? Color.red : Color.yellow;
                DrawCircle(t.position, cfg.aggroRadius);

                if (aggro)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f);
                    DrawCircle(t.position, cfg.loseRadius);
                }

                if (bb != null)
                {
                    Gizmos.color = Color.cyan;
                    DrawCircle(bb.HomePosition, cfg.leashDistance);
                    Gizmos.DrawLine(bb.HomePosition, bb.HomePosition + Vector3.up * 1.5f);
                }

                AbilityDefinition ability = cfg.primaryAbility;
                if (ability != null)
                {
                    Gizmos.color = Color.green;
                    DrawCircle(t.position, ability.maxRange);

                    if (ability.minRange > 0f)
                    {
                        Gizmos.color = new Color(0f, 0.5f, 0f);
                        DrawCircle(t.position, ability.minRange);
                    }

                    if (ability.kind == AbilityKind.Melee)
                    {
                        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
                        Gizmos.DrawWireSphere(
                            t.position + t.forward * ability.hitForwardOffset + Vector3.up * ability.hitHeight,
                            ability.hitRadius);
                    }
                }
            }

            if (bb != null && bb.HasTarget)
            {
                Gizmos.color = bb.IsAggro ? Color.red : Color.gray;
                Gizmos.DrawLine(t.position + Vector3.up, bb.Target.position + Vector3.up);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(t.position + Vector3.up * 2.4f, p.LastState + "\n" + p.Path);
#endif
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            if (radius <= 0.01f) return;

            center.y += 0.05f;
            const int segments = 32;

            Vector3 previous = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
