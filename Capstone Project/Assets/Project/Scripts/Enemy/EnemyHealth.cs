using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Mhieu.Enemy
{
    public class EnemyHealth : CharacterRuntime
    {
        private static readonly int Hit = Animator.StringToHash("Hit");
        private readonly List<IEffect<IAttackable>> _activeEffects = new();
        
        [Header("Setting")] 
        [SerializeField] private Image healthBar;
        [SerializeField] private ParticleSystem impactParticles;
        [SerializeField] private Animator animator;
        
        [Header("Sounds")]
        [SerializeField] private AudioCue hurtCue;
        [SerializeField] private AudioCue deathCue;

        private float _targetFillAmount = 1f;

        private void Awake()
        {
            if(animator == null) animator = GetComponentInChildren<Animator>();
            Init();
            
            _targetFillAmount = TotalHealth > 0 ? (float)Hp / TotalHealth : 1f;
            if(healthBar != null) healthBar.fillAmount = _targetFillAmount;
        }

        private void Update()
        {
            UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            if(healthBar == null) return;
            
            healthBar.fillAmount = Mathf.MoveTowards(
                healthBar.fillAmount, 
                _targetFillAmount, 
                2f * Time.deltaTime
            );
        }

        public override void TakeDamage(float damage, ICharacterRuntime runtime)
        {
            base.TakeDamage(damage, runtime);
            _targetFillAmount = TotalHealth > 0 ? Mathf.Clamp01((float)Hp / TotalHealth) : 0f;
            
            if(Hp <= 0) return;
            if(hurtCue != null) AudioService.Instance?.PlaySfx(hurtCue, transform.position);
        }

        protected override void OnTakeDamage(float damage)
        {
            base.OnTakeDamage(damage);
            if(animator != null) animator.SetTrigger(Hit);
            if(impactParticles != null) Instantiate(impactParticles, transform.position + Vector3.up, transform.rotation);
        }

        public void ApplyEffect(IEffect<IAttackable> effect)
        {
            // Delete old one if new effect have same type
            if (effect is IStatusEffect newStatusEffect)
            {
                var existing = _activeEffects.Find(e 
                    => e is IStatusEffect s && s.StatusType == newStatusEffect.StatusType);

                if (existing != null)
                {
                    existing.OnCompleted -= RemoveEffect;
                    _activeEffects.Remove(existing);
                    existing.Cancel();
                }
            }
            
            // Apply new effect
            effect.OnCompleted += RemoveEffect;
            _activeEffects.Add(effect);
            effect.Apply(this);
        }

        private void RemoveEffect(IEffect<IAttackable> effect)
        {
            effect.OnCompleted -= RemoveEffect;
            _activeEffects.Remove(effect);
        }

        public override void Die()
        {
            base.Die();
            if(deathCue != null) AudioService.Instance?.PlaySfx(deathCue, transform.position);
            
            // Handle enemy death logic here (e.g., play animation, drop loot, etc.)
            // Remove all applied effect in this enemy
            foreach (var effect in _activeEffects)
            {
                effect.OnCompleted -= RemoveEffect;
                effect.Cancel();
            }
            _activeEffects.Clear();
            
            gameObject.SetActive(false); // For now, just deactivate the enemy
        }
    }
}
