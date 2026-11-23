using System.Reflection;
using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Khi boss hết giao chiến đủ lâu (mất aggro và không bị đánh trong resetDelay giây) thì:
    ///  - hồi đầy máu (gọi CharacterRuntime.Heal) và cập nhật thanh máu,
    ///  - đưa về pha 1 (BossPhaseController.ResetToPhaseOne).
    /// Nhờ vậy player chạy ra xa rồi quay lại không thể "kite" boss để lột pha, và boss không giữ máu thấp mãi.
    ///
    /// Không reset khi: boss đã chết, đang chuyển pha, đang bị đánh gần đây, hoặc đã đầy máu ở pha 1.
    /// Chỉ reset MỘT lần cho mỗi lần thoát giao chiến.
    ///
    /// Đặt cùng object với BossHealth, EnemyBrain, BossPhaseController.
    ///
    /// Lưu ý về thanh máu: EnemyHealth (code của bạn cùng nhóm) chỉ cập nhật thanh máu trong TakeDamage,
    /// còn Heal() thì không. Script này đặt trường private "_targetFillAmount" của EnemyHealth về 1 bằng
    /// reflection để thanh máu tự chạy đầy lại. Nếu sau này EnemyHealth đổi tên trường đó, script chỉ cảnh báo
    /// một lần (máu vẫn hồi đúng, chỉ thanh máu không đầy lại cho tới đòn trúng kế tiếp).
    /// </summary>
    [DisallowMultipleComponent]
    public class BossLeashReset : MonoBehaviour
    {
        [Tooltip("Số giây mất aggro (và không bị đánh) trước khi reset.")]
        [SerializeField, Min(0.5f)] private float resetDelay = 5f;
        [Tooltip("Ghi Console mỗi lần reset.")]
        [SerializeField] private bool logReset = true;

        private EnemyBrain _brain;
        private CharacterRuntime _runtime;
        private BossPhaseController _phase;
        private IStateMachine _stateMachine;

        private float _outOfCombatSince = -1f;
        private bool _alreadyReset;
        private bool _warnedBar;

        private static readonly FieldInfo FillField =
            typeof(EnemyHealth).GetField("_targetFillAmount", BindingFlags.Instance | BindingFlags.NonPublic);

        private void Awake()
        {
            _brain = GetComponent<EnemyBrain>();
            _runtime = GetComponent<CharacterRuntime>();
            _phase = GetComponent<BossPhaseController>();
            _stateMachine = GetComponent<IStateMachine>();
        }

        private void OnEnable()
        {
            _outOfCombatSince = -1f;
            _alreadyReset = false;
        }

        private void Update()
        {
            if (_brain == null || _brain.Blackboard == null || _runtime == null) return;
            if (_runtime.Health <= 0) return;
            if (_stateMachine != null && _stateMachine.IsCurrentState(CharacterStateType.Dead)) return;

            var bb = _brain.Blackboard;

            if (bb.IsAggro)
            {
                _outOfCombatSince = -1f;
                _alreadyReset = false;
                return;
            }

            if (_alreadyReset) return;
            if (_phase != null && _phase.IsTransitioning) return;

            if (_outOfCombatSince < 0f) _outOfCombatSince = Time.time;

            bool calmLongEnough = Time.time - _outOfCombatSince >= resetDelay
                                  && Time.time - bb.LastDamagedTime >= resetDelay;
            if (!calmLongEnough) return;

            _alreadyReset = true;
            ResetBoss();
        }

        private void ResetBoss()
        {
            bool needsHeal = _runtime.Health < _runtime.TotalHealth;
            bool needsPhase = _phase != null && _phase.Phase > 1;
            if (!needsHeal && !needsPhase) return;

            if (needsHeal)
            {
                _runtime.Heal(_runtime.TotalHealth);
                RefillHealthBar();
            }

            if (needsPhase) _phase.ResetToPhaseOne();

            if (logReset)
                UnityEngine.Debug.Log($"[BossLeashReset] {name}: hồi đầy máu và về pha 1 sau {resetDelay:0.#}s hết giao chiến.", this);
        }

        private void RefillHealthBar()
        {
            // BossHealth tự cập nhật thanh máu qua OnHpChanged (Heal đã phát sự kiện này), không cần reflection.
            if (_runtime is BossHealth) return;

            // Ép về object trước khi "as" để compile được dù EnemyHealth trong dự án là bản nào.
            EnemyHealth health = (object)_runtime as EnemyHealth;
            if (health == null || FillField == null)
            {
                WarnBarOnce();
                return;
            }

            FillField.SetValue(health, 1f);
        }

        private void WarnBarOnce()
        {
            if (_warnedBar) return;
            _warnedBar = true;
            UnityEngine.Debug.LogWarning(
                "[BossLeashReset] Không cập nhật được thanh máu (không tìm thấy EnemyHealth._targetFillAmount). " +
                "Máu đã hồi đủ nhưng thanh máu chỉ đầy lại sau đòn trúng kế tiếp.", this);
        }
    }
}
