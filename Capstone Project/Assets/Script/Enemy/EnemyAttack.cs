using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    public float attackDamage = 10f;

    public float attackCooldown = 1.5f;

    [Header("Attack Range")]
    public float attackRange = 2f;

    private float lastAttackTime = -999f;

    public bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    public void Attack(GameObject target)
    {
        if (!CanAttack())
            return;

        lastAttackTime = Time.time;

        if (target == null)
            return;

        PlayerHealth playerHealth =
            target.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }

        Debug.Log(
            gameObject.name +
            " tấn công Player gây " +
            attackDamage +
            " damage."
        );
    }
}
