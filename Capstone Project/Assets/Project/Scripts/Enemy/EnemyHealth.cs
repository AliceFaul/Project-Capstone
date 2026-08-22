using System;
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

        private float _targetFillAmount = 1f;

        private void Awake()
        {
            if(animator == null)
                animator = GetComponentInChildren<Animator>();
            
            Init();
            
            _targetFillAmount = 1f;
            healthBar.fillAmount = 1;
        }

        private void Update()
        {
            UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            healthBar.fillAmount = Mathf.MoveTowards(
                healthBar.fillAmount, 
                _targetFillAmount, 
                1.5f * Time.deltaTime
            );
        }

        public override void TakeDamage(float damage, ICharacterRuntime runtime)
        {
            base.TakeDamage(damage, runtime);
            _targetFillAmount = Hp / TotalHealth;
        }

        protected override void OnTakeDamage(float damage)
        {
            base.OnTakeDamage(damage);
            animator.SetTrigger(Hit);
            Instantiate(impactParticles, transform.position + Vector3.up, transform.rotation);
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
