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

        private void Awake()
        {
            if(animator == null)
                animator = GetComponentInChildren<Animator>();
            
            _currentHealth = maxHealth;
            UpdateHealthBar(_currentHealth / maxHealth);
        }

        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, maxHealth);
            
            OnTakeDamage();

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        private void OnTakeDamage()
        {
            animator.SetTrigger(Hit);
            UpdateHealthBar(_currentHealth / maxHealth);
            Instantiate(impactParticles, transform.position + Vector3.up, transform.rotation);
            // Damage flash
        }

        private void UpdateHealthBar(float amount)
        {
            healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, amount, Time.deltaTime * 1.5f);
        }

        private void Die()
        {
            // Handle enemy death logic here (e.g., play animation, drop loot, etc.)
            gameObject.SetActive(false); // For now, just deactivate the enemy
        }
    }
}
