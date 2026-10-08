using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Retreat")]
    [Range(0.01f, 1f)]
    public float retreatHealthPercent = 0.2f;

    private EnemyFSM enemyFSM;

    private void Awake()
    {
        currentHealth = maxHealth;
        enemyFSM = GetComponent<EnemyFSM>();
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0)
            return;

        currentHealth -= damage;

        Debug.Log(gameObject.name + " nhận " + damage + " damage.");

        // Chết
        if (currentHealth <= 0)
        {
            currentHealth = 0;

            enemyFSM.ChangeState(EnemyState.Dead);
            return;
        }

        // Máu thấp
        if (currentHealth <= maxHealth * retreatHealthPercent)
        {
            enemyFSM.ChangeState(EnemyState.Retreat);
            return;
        }

        // Bị đánh
        enemyFSM.ChangeState(EnemyState.Hit);
    }
}