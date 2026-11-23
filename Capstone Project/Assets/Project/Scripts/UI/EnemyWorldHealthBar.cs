using UnityEngine;
using UnityEngine.UI;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Thanh máu nổi trên đầu enemy (World Space), tự dựng bằng code, luôn quay mặt về camera.
    ///
    /// Đọc máu qua ICharacterRuntime (Health / TotalHealth) mỗi frame nên chạy với mọi enemy kế thừa CharacterRuntime,
    /// không cần gán Image hay Canvas. Gồm: nền tối, lớp "máu vừa mất" màu vàng trượt xuống chậm,
    /// và lớp máu hiện tại đổi màu từ xanh sang đỏ theo tỉ lệ.
    ///
    /// Dùng lại được cho pool: bật lại object thì thanh máu tự đặt lại theo máu mới.
    /// Thanh máu là object riêng ở ngoài enemy (không bị scale / xoay của enemy làm méo),
    /// tự bật tắt theo enemy và tự hủy khi enemy bị hủy.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyWorldHealthBar : MonoBehaviour
    {
        [Header("Vị trí và kích thước (mét)")]
        [Tooltip("Cao hơn chân enemy bao nhiêu. Quái nhỏ khoảng 1.5, boss cao thì 4-5.")]
        [SerializeField] private float heightOffset = 2.2f;
        [SerializeField, Min(0.1f)] private float width = 1.2f;
        [SerializeField, Min(0.02f)] private float height = 0.14f;

        [Header("Hiển thị")]
        [Tooltip("Bật: chỉ hiện khi enemy đã mất máu. Tắt: luôn hiện.")]
        [SerializeField] private bool hideWhenFull;
        [SerializeField] private Color fullColor = new Color(0.25f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color lowColor = new Color(0.9f, 0.15f, 0.12f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private Color trailColor = new Color(1f, 0.85f, 0.25f, 1f);

        [Header("Hiệu ứng máu vừa mất")]
        [SerializeField, Min(0f)] private float trailDelay = 0.35f;
        [SerializeField, Min(0.05f)] private float trailSpeed = 0.6f;

        private HealthSource _source;
        private GameObject _root;
        private Image _fill;
        private Image _trail;
        private Camera _camera;

        private bool _ready;
        private float _ratio = 1f;
        private float _trailRatio = 1f;
        private float _trailHoldUntil;

        private void Awake()
        {
            _source = HealthSource.Find(gameObject);
            if (_source == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[EnemyWorldHealthBar] Không tìm thấy nguồn máu trên '" + name + "' (cần ICharacterRuntime, hoặc component có " +
                    "trường currentHealth và maxHealth) nên không hiện được thanh máu.", this);
                enabled = false;
                return;
            }

            Build();
        }

        // Máu khởi tạo ở Awake của component máu, nên chỉ đọc từ Start trở đi.
        private void Start()
        {
            _ready = true;
            Snap();
            ApplyVisibility();
            UnityEngine.Debug.Log("[EnemyWorldHealthBar] '" + name + "' đọc máu từ " + _source.Description +
                                  " (" + _source.Current + "/" + _source.Max + ")", this);
        }

        private void OnEnable()
        {
            if (_ready)
            {
                Snap();
                ApplyVisibility();
            }
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

            float target = _source.Ratio;

            if (target < _ratio - 0.0001f) _trailHoldUntil = Time.time + trailDelay;
            _ratio = target;

            if (_trailRatio < _ratio) _trailRatio = _ratio;
            else if (Time.time >= _trailHoldUntil)
                _trailRatio = Mathf.MoveTowards(_trailRatio, _ratio, trailSpeed * Time.deltaTime);

            HealthBarUtil.SetFill(_fill, _ratio);
            HealthBarUtil.SetFill(_trail, _trailRatio);
            _fill.color = Color.Lerp(lowColor, fullColor, _ratio);

            ApplyVisibility();
        }

        private void LateUpdate()
        {
            if (_root == null || !_root.activeSelf) return;

            _root.transform.position = transform.position + Vector3.up * heightOffset;

            if (_camera == null) _camera = Camera.main;
            if (_camera == null) _camera = FindFirstObjectByType<Camera>();
            if (_camera != null) _root.transform.rotation = _camera.transform.rotation;
        }

        private void Snap()
        {
            _ratio = _source.Ratio;
            _trailRatio = _ratio;
            HealthBarUtil.SetFill(_fill, _ratio);
            HealthBarUtil.SetFill(_trail, _trailRatio);
            _fill.color = Color.Lerp(lowColor, fullColor, _ratio);
        }

        private void ApplyVisibility()
        {
            bool alive = _source.Alive;
            bool show = alive && (!hideWhenFull || _ratio < 0.999f);
            if (_root.activeSelf != show) _root.SetActive(show);
        }

        private void Build()
        {
            _root = new GameObject(name + "_HealthBar");

            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            // 1 đơn vị canvas = 1/100 mét.
            const float pixelsPerMeter = 100f;
            var rect = (RectTransform)_root.transform;
            rect.sizeDelta = new Vector2(width * pixelsPerMeter, height * pixelsPerMeter);
            rect.localScale = Vector3.one / pixelsPerMeter;

            HealthBarUtil.CreateImage("Background", _root.transform, backgroundColor);

            RectTransform inner = HealthBarUtil.CreateRect("Inner", _root.transform);
            HealthBarUtil.Stretch(inner, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            _trail = HealthBarUtil.CreateImage("Trail", inner, trailColor);
            _fill = HealthBarUtil.CreateImage("Fill", inner, fullColor);

            _root.SetActive(false);
        }
    }
}
