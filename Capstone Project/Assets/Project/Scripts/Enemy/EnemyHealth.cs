using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mhieu.Enemy
{
    public class EnemyHealth : MonoBehaviour, IAttackable
    {
        private static readonly int Hit = Animator.StringToHash("Hit");

        [Header("Setting")] 
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private Image healthBar;
        [SerializeField] private ParticleSystem impactParticles;
        [SerializeField] private Animator animator;

        private float _currentHealth;
        private float _targetFillAmount = 1f;

        private void Awake()
        {
            if(animator == null)
                animator = GetComponentInChildren<Animator>();
            
            _currentHealth = maxHealth;
            _targetFillAmount = 1f;
            
            healthBar.fillAmount = 1;
        }

        private void Update()
        {
            UpdateHealthBar();
        }

        public void TakeDamage(float damage)
        {
            _currentHealth = Mathf.Clamp(_currentHealth - damage, 0f, maxHealth);
            _targetFillAmount = _currentHealth / maxHealth;
            
            OnTakeDamage();

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        private void OnTakeDamage()
        {
            animator.SetTrigger(Hit);
            Instantiate(impactParticles, transform.position + Vector3.up, transform.rotation);
            // Damage flash
        }

        private void UpdateHealthBar()
        {
            healthBar.fillAmount = Mathf.MoveTowards(
                healthBar.fillAmount, 
                _targetFillAmount, 
                1.5f * Time.deltaTime
            );
        }

        private void Die()
        {
            // Handle enemy death logic here (e.g., play animation, drop loot, etc.)
            gameObject.SetActive(false); // For now, just deactivate the enemy
        }
    }
}
