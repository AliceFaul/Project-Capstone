using UnityEngine;
using UnityEngine.UI;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Thanh máu boss lớn ở đỉnh màn hình (Screen Space Overlay), tự dựng bằng code.
    ///
    /// Hiện (mờ dần vào) khi boss đang giao chiến (có aggro) hoặc khi bật Always Show; ẩn khi boss chết,
    /// mất aggro hoặc bị tắt. Gồm: tên boss, số máu, nhãn pha (BossPhaseController), thanh máu có lớp "máu vừa mất"
    /// và vạch đánh dấu các mốc chuyển pha.
    ///
    /// Đặt cùng object với BossHealth + EnemyBrain. Canvas là object riêng ở ngoài boss, tự hủy cùng boss.
    /// Dùng font dựng sẵn của Unity (chữ không dấu hiển thị chắc chắn; chữ có dấu tiếng Việt tùy font).
    /// </summary>
    [DisallowMultipleComponent]
    public class BossScreenHealthBar : MonoBehaviour
    {
        [Header("Nội dung")]
        [SerializeField] private string displayName = "BOSS";
        [Tooltip("Vạch đánh dấu các mốc máu chuyển pha (0..1). Nên khớp Phase Thresholds của BossPhaseController.")]
        [SerializeField] private float[] phaseMarkers = { 0.66f, 0.33f };
        [Tooltip("Bật: luôn hiện khi boss còn sống. Tắt: chỉ hiện khi boss đang giao chiến.")]
        [SerializeField] private bool alwaysShow;

        [Header("Kích thước (pixel, màn hình chuẩn 1920x1080)")]
        [SerializeField, Min(100f)] private float barWidth = 900f;
        [SerializeField, Min(10f)] private float barHeight = 26f;
        [SerializeField] private float topMargin = 40f;

        [Header("Màu")]
        [SerializeField] private Color fillColor = new Color(0.85f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
        [SerializeField] private Color trailColor = new Color(1f, 0.85f, 0.25f, 1f);

        [Header("Hiệu ứng")]
        [SerializeField, Min(0f)] private float trailDelay = 0.4f;
        [SerializeField, Min(0.05f)] private float trailSpeed = 0.5f;
        [SerializeField, Min(0.5f)] private float fadeSpeed = 4f;

        private ICharacterRuntime _runtime;
        private EnemyBrain _brain;
        private BossPhaseController _phase;

        private GameObject _root;
        private CanvasGroup _group;
        private Image _fill;
        private Image _trail;
        private Text _healthText;
        private Text _phaseText;

        private bool _ready;
        private float _ratio = 1f;
        private float _trailRatio = 1f;
        private float _trailHoldUntil;

        private void Awake()
        {
            _runtime = GetComponent<ICharacterRuntime>();
            _brain = GetComponent<EnemyBrain>();
            _phase = GetComponent<BossPhaseController>();

            if (_runtime == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[BossScreenHealthBar] Object này không có ICharacterRuntime (BossHealth) nên không hiện được thanh máu.", this);
                enabled = false;
                return;
            }

            Build();
        }

        private void Start()
        {
            _ready = true;
            Snap();
            _group.alpha = 0f;
        }

        private void OnEnable()
        {
            if (_ready) Snap();
            if (_root != null) _root.SetActive(true);
        }

        private void OnDisable()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_root != null) Destroy(_root);
        }

        private void Update()
        {
            if (!_ready) return;

            float target = HealthBarUtil.Ratio(_runtime);
            if (target < _ratio - 0.0001f) _trailHoldUntil = Time.time + trailDelay;
            _ratio = target;

            if (_trailRatio < _ratio) _trailRatio = _ratio;
            else if (Time.time >= _trailHoldUntil)
                _trailRatio = Mathf.MoveTowards(_trailRatio, _ratio, trailSpeed * Time.deltaTime);

            HealthBarUtil.SetFill(_fill, _ratio);
            HealthBarUtil.SetFill(_trail, _trailRatio);

            _healthText.text = _runtime.Health + " / " + _runtime.TotalHealth;
            _phaseText.text = _phase != null ? "PHASE " + _phase.Phase : string.Empty;

            bool inCombat = _brain != null && _brain.Blackboard != null && _brain.Blackboard.IsAggro;
            bool show = _runtime.Health > 0 && (alwaysShow || inCombat);

            _group.alpha = Mathf.MoveTowards(_group.alpha, show ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
        }

        private void Snap()
        {
            _ratio = HealthBarUtil.Ratio(_runtime);
            _trailRatio = _ratio;
            HealthBarUtil.SetFill(_fill, _ratio);
            HealthBarUtil.SetFill(_trail, _trailRatio);
        }

        private void Build()
        {
            _root = new GameObject(name + "_BossHealthBar");

            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Khung chứa: neo giữa đỉnh màn hình.
            RectTransform container = HealthBarUtil.CreateRect("Container", _root.transform);
            container.anchorMin = new Vector2(0.5f, 1f);
            container.anchorMax = new Vector2(0.5f, 1f);
            container.pivot = new Vector2(0.5f, 1f);
            container.sizeDelta = new Vector2(barWidth, barHeight + 30f);
            container.anchoredPosition = new Vector2(0f, -topMargin);
            _group = container.gameObject.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;

            // Nửa trên: tên (trái), nhãn pha (phải). Nửa dưới: thanh máu.
            float textShare = 30f / (barHeight + 30f);

            RectTransform nameRow = HealthBarUtil.CreateRect("NameRow", container);
            HealthBarUtil.Stretch(nameRow, new Vector2(0f, 1f - textShare), Vector2.one, Vector2.zero, Vector2.zero);

            Text nameText = HealthBarUtil.CreateText("Name", nameRow, 22, TextAnchor.MiddleLeft, Color.white);
            nameText.text = displayName;
            _phaseText = HealthBarUtil.CreateText("Phase", nameRow, 18, TextAnchor.MiddleRight,
                new Color(1f, 0.85f, 0.25f, 1f));

            RectTransform barRow = HealthBarUtil.CreateRect("BarRow", container);
            HealthBarUtil.Stretch(barRow, Vector2.zero, new Vector2(1f, 1f - textShare), Vector2.zero, Vector2.zero);

            HealthBarUtil.CreateImage("Background", barRow, backgroundColor);

            RectTransform inner = HealthBarUtil.CreateRect("Inner", barRow);
            HealthBarUtil.Stretch(inner, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));

            _trail = HealthBarUtil.CreateImage("Trail", inner, trailColor);
            _fill = HealthBarUtil.CreateImage("Fill", inner, fillColor);

            if (phaseMarkers != null)
            {
                foreach (float marker in phaseMarkers)
                {
                    if (marker <= 0f || marker >= 1f) continue;

                    Image tick = HealthBarUtil.CreateImage("PhaseMarker", inner, new Color(1f, 1f, 1f, 0.85f));
                    HealthBarUtil.Stretch(tick.rectTransform, new Vector2(marker, 0f), new Vector2(marker, 1f),
                        new Vector2(-1f, 0f), new Vector2(1f, 0f));
                }
            }

            _healthText = HealthBarUtil.CreateText("HealthText", barRow, 16, TextAnchor.MiddleCenter, Color.white);
        }
    }
}
