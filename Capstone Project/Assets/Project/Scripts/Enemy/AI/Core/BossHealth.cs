using UnityEngine;
using UnityEngine.UI;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Máu của BOSS. Kế thừa thẳng CharacterRuntime (giống player) nên KHÔNG phụ thuộc vào bất kỳ
    /// file EnemyHealth nào trong dự án (có hai bản EnemyHealth khác nhau đang lưu hành).
    ///
    /// Nhờ kế thừa CharacterRuntime, boss có đủ: ICharacterRuntime (Health, TotalHealth, TotalDamage,
    /// OnHit...) và IAttackable (đòn của player gọi TakeDamage qua interface này).
    /// Cây hành vi, BossPhaseController, AbilityRunner, BossLeashReset đều dựa vào các thứ đó.
    ///
    /// Việc làm thêm so với CharacterRuntime:
    ///  - Bất tử khi IsInvulnerable = true (BossPhaseController bật trong lúc chuyển pha). Dùng override
    ///    nên đòn gọi qua IAttackable cũng bị chặn.
    ///  - Thanh máu (Image Fill) mượt, tự cập nhật theo OnHpChanged: trúng đòn, hồi máu, reset đều đúng.
    ///  - Hạt trúng đòn (tùy chọn).
    ///  - Chết: CharacterRuntime.Die() đổi state sang Dead (cây hành vi dừng, EnemyMover dừng),
    ///    rồi tắt object. EnemyDeathVisual (nếu có trên prefab) kịp tạo xác trước khi tắt.
    ///
    /// Trên prefab boss: máu tối đa lấy từ CharacterData trong Character Install (baseHealth phải lớn hơn 0).
    /// </summary>
    public class BossHealth : CharacterRuntime
    {
        [Header("UI")]
        [SerializeField] private Image healthBar;
        [SerializeField, Min(0.1f)] private float barSpeed = 1.5f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem impactParticles;

        private float _targetFill = 1f;

        public bool IsInvulnerable { get; set; }

        private void Awake()
        {
            Init();

            OnHpChanged += HandleHpChanged;
            HandleHpChanged(Hp);

            if (healthBar != null) healthBar.fillAmount = _targetFill;
        }

        private void OnDestroy()
        {
            OnHpChanged -= HandleHpChanged;
        }

        private void Update()
        {
            if (healthBar == null) return;

            healthBar.fillAmount = Mathf.MoveTowards(healthBar.fillAmount, _targetFill, barSpeed * Time.deltaTime);
        }

        public override void TakeDamage(float damage, ICharacterRuntime runtime)
        {
            if (IsInvulnerable) return;
            if (Hp <= 0) return; // đã chết: không nhận thêm, không Die / thưởng kinh nghiệm lần nữa

            base.TakeDamage(damage, runtime);
        }

        protected override void OnTakeDamage(float damage)
        {
            base.OnTakeDamage(damage);

            if (impactParticles != null)
                Instantiate(impactParticles, transform.position + Vector3.up, transform.rotation);
        }

        public override void Die()
        {
            base.Die();

            _targetFill = 0f;
            gameObject.SetActive(false);
        }

        // ---- Công cụ test: chuột phải vào tên component "Boss Health" trong Inspector (khi đang Play) ----

        [ContextMenu("Debug: Hạ 30% máu")]
        private void DebugDamageThirty()
        {
            TakeDamage(TotalHealth * 0.3f + TotalArmor, null);
        }

        [ContextMenu("Debug: Giết boss ngay")]
        private void DebugKill()
        {
            bool wasInvulnerable = IsInvulnerable;
            IsInvulnerable = false;
            TakeDamage(TotalHealth * 100f + TotalArmor, null);
            IsInvulnerable = wasInvulnerable;
        }

        private void HandleHpChanged(int hp)
        {
            int total = TotalHealth;
            _targetFill = total > 0 ? Mathf.Clamp01((float)hp / total) : 0f;
        }
    }
}
