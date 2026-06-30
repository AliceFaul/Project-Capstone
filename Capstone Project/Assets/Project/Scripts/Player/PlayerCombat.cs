using UnityEngine;

public class PlayerCombat : MonoBehaviour {
    [Header("Setting")]
    [SerializeField] public float attackRange = 2f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private LayerMask enemyLayer;

    private float lastAttackTime;

    // Call this method in the PlayerController when the player clicks on an enemy
    // This method will check if the player can attack and then perform the attack
    // TODO: You can add an animation trigger here if you have an attack animation
    public void Attack() {
        if(Time.time - lastAttackTime < attackCooldown) return;
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
        foreach(Collider enemy in hitEnemies) {
            // Check if the enemy has an IAttackable component and call TakeDamage
            IAttackable attackable = enemy.GetComponent<IAttackable>();
            if(attackable != null) {
                attackable.TakeDamage(attackDamage);
                Debug.Log($"Attacked {enemy.name} for {attackDamage} damage.");
            } else {
                Debug.LogWarning($"Enemy {enemy.name} does not implement IAttackable.");
            }
        }
        lastAttackTime = Time.time;
    }

    public void OnDrawGizmosSelected() {
        if(attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
