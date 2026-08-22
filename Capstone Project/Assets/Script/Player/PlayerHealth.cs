using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    public float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log(
            "Player HP: " +
            currentHealth
        );

        if (currentHealth <= 0)
        {
            currentHealth = 0;

            Die();
        }
    }

    private void Die()
    {
        Debug.Log("PLAYER DEAD");
    }
}