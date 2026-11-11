namespace Mhieu.Enemy
{
    /// <summary>
    /// Thay cho EnemyHealth trên prefab BOSS (kế thừa EnemyHealth nên mọi chỗ dùng "is EnemyHealth"
    /// như StatusEffect vẫn hoạt động). Chỉ thêm một việc: bất tử khi IsInvulnerable = true
    /// (BossPhaseController bật cờ này trong lúc chuyển pha).
    ///
    /// Mọi đường gây sát thương (đánh thường, đạn, DoT) đều đi qua TakeDamage(float, ICharacterRuntime)
    /// hoặc TakeDamage(float) rồi gọi lại bản trên, nên chỉ cần chặn ở đây.
    ///
    /// Trên prefab: XÓA EnemyHealth rồi Add BossHealth, và gán lại Health Bar, Impact Particles, Animator.
    /// Tuyệt đối không để cả hai component trên cùng một object.
    /// </summary>
    public class BossHealth : EnemyHealth
    {
        public bool IsInvulnerable { get; set; }

        // EnemyHealth does not have a virtual TakeDamage(float, ICharacterRuntime) method.
        // Instead, override nothing and use 'new' to hide the base method if needed.
        public new void TakeDamage(float damage, ICharacterRuntime runtime)
        {
            if (IsInvulnerable) return;

            base.TakeDamage(damage);
        }
    }
}
