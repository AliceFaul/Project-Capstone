using UnityEngine;

namespace Mhieu.Enemy
{
    public class EnemyHealth : MonoBehaviour, IAttackable
    {
        [Header("Setting")] [SerializeField] private float maxHealth = 100f;

        private float _currentHealth;

        private void Awake()
        {
            _currentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, maxHealth);

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            // Handle enemy death logic here (e.g., play animation, drop loot, etc.)
            gameObject.SetActive(false); // For now, just deactivate the enemy
        }
    }
}
