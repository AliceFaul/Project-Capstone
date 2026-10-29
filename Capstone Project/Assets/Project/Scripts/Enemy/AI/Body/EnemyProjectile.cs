using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Đạn của enemy (tách riêng khỏi Projectile của player).
    ///
    /// Không cần Collider hay Rigidbody trên prefab: đạn tự dò va chạm bằng SphereCast mỗi frame
    /// nên không xuyên qua mục tiêu khi bay nhanh. Prefab chỉ cần script này + phần hiển thị.
    ///
    /// Dùng Time.deltaTime (có scale) nên hit-stop của player cũng làm đạn đứng hình.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float radius = 0.25f;
        [Tooltip("Layer chắn đạn (tường, vật cản). KHÔNG chọn layer ground nếu đạn bay thấp sát đất.")]
        [SerializeField] private LayerMask obstacleLayers;
        [Tooltip("Hiệu ứng khi trúng (tùy chọn).")]
        [SerializeField] private GameObject impactPrefab;

        private readonly Collider[] _overlapBuffer = new Collider[4];

        private Vector3 _direction;
        private float _damage;
        private float _speed;
        private ICharacterRuntime _owner;
        private LayerMask _targetLayers;
        private bool _initialized;

        public void Initialize(Vector3 direction, float damage, float speed,
            ICharacterRuntime owner, LayerMask targetLayers, float lifetime)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
            _damage = damage;
            _speed = speed;
            _owner = owner;
            _targetLayers = targetLayers;
            _initialized = true;

            transform.rotation = Quaternion.LookRotation(_direction);
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (!_initialized) return;

            int mask = _targetLayers.value | obstacleLayers.value;

            // Đang nằm sẵn trong collider (bắn sát mặt): SphereCast không thấy, nên kiểm tra overlap trước.
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _overlapBuffer, mask,
                QueryTriggerInteraction.Ignore);
            if (count > 0)
            {
                HandleHit(_overlapBuffer[0], transform.position);
                return;
            }

            float step = _speed * Time.deltaTime;
            if (step <= 0f) return;

            if (Physics.SphereCast(transform.position, radius, _direction, out RaycastHit hit, step, mask,
                    QueryTriggerInteraction.Ignore))
            {
                HandleHit(hit.collider, hit.point);
                return;
            }

            transform.position += _direction * step;
        }

        private void HandleHit(Collider collider, Vector3 point)
        {
            bool isTarget = ((1 << collider.gameObject.layer) & _targetLayers.value) != 0;
            if (isTarget)
            {
                IAttackable attackable = collider.GetComponentInParent<IAttackable>();
                attackable?.TakeDamage(_damage, _owner);
            }

            if (impactPrefab != null)
                Instantiate(impactPrefab, point, Quaternion.LookRotation(-_direction));

            _initialized = false;
            Destroy(gameObject);
        }
    }
}
