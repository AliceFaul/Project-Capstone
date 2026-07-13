using System;
using TMPro;
using UnityEngine;

public class PlayerCombat : MonoBehaviour {
    [Header("Melee Setting")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 10;
    
    [Header("Ranged Setting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRange = 20f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootCooldown = 1f;
    
    [Header("Ammo Setting")]
    [SerializeField] private int maxAmmo = 15;
    [SerializeField] private TMP_Text ammoText;
    
    [SerializeField] private LayerMask enemyLayer;
    
    private Transform _currentTarget;
    private int _currentAmmo; 
        
    public float AttackRange => attackRange;
    private float _lastAttackTime;
    private float _lastShootTime;

    private void Start()
    {
        _currentAmmo = maxAmmo;
        if (ammoText != null)
        {
            ammoText.text = _currentAmmo.ToString();
        }
    }

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
                Debug.Log($"[Melee Attack]: Attacked {enemy.name} for {attackDamage} damage.");
            } else {
                Debug.LogWarning($"Enemy {enemy.name} does not implement IAttackable.");
            }
        }
        _currentTarget = null;
        _lastAttackTime = Time.time;
    }
    
    // Call this method in the PlayerController when the player right clicks
    // TODO: Add animation trigger here and ammo
    public void Shoot(Vector3 mousePosition)
    {
        if(_currentAmmo <= 0) 
            return;
        if(Time.time - _lastShootTime < shootCooldown) 
            return;
        
        Vector3 direction = mousePosition - firePoint.position;
        direction.y = 0f;
        transform.forward = direction.normalized;
        
        var projectile =  Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        
        projectile.GetComponent<Projectile>().Initialize(direction);
        
        AdjustAmmo(-1);
        _lastShootTime = Time.time;
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

    private void AdjustAmmo(int amount = 1)
    {
        _currentAmmo += amount;
        ammoText.text = _currentAmmo.ToString();
    }

    public void OnDrawGizmosSelected() {
        if(attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
