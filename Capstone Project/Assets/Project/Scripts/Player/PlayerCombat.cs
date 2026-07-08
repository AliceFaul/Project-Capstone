using UnityEngine;

public class PlayerCombat : MonoBehaviour {
    [Header("Setting")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private LayerMask enemyLayer;
    
    private Transform _currentTarget;
    public float AttackRange => attackRange;
    private float _lastAttackTime;

    public void SetTarget(Transform target) => _currentTarget = target;

    // Call this method in the PlayerController when the player clicks on an enemy
    // This method will check if the player can attack and then perform the attack
    // TODO: You can add an animation trigger here if you have an attack animation
    public void Attack() {
        if(_currentTarget == null) 
            return;
        if(Time.time - _lastAttackTime < attackCooldown) 
            return;
        RotateToTarget();
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
        _currentTarget = null;
        _lastAttackTime = Time.time;
    }

    private void RotateToTarget()
    {
        Vector3 direction = _currentTarget.transform.position - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }
        
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = lookRotation;
    }

    public void OnDrawGizmosSelected() {
        if(attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
