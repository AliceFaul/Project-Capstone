using System;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerCombat : MonoBehaviour {
    [Header("Combat Setting")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    
    [Header("Ammo Setting")]
    [SerializeField] private int maxAmmo = 15;
    [SerializeField] private TMP_Text ammoText;
    
    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayer;
    
    private Transform _currentTarget;
    private int _currentAmmo; 
    
    private PlayerRuntime _runtime;
    
    public float AttackRange => _runtime.AttackRange;
    
    private float _lastAttackTime;
    private float _lastShootTime;

    private void Awake()
    {
        _runtime = GetComponent<PlayerRuntime>();
    }

    private void Start()
    {
        _currentAmmo = maxAmmo;
        if (ammoText != null)
        {
            ammoText.text = _currentAmmo.ToString();
        }
    }

    public void SetTarget(Transform target)
    {
        _currentTarget = target;
        Debug.Log($"Target: {target.name}");
    }

    // Call this method in the PlayerController when the player clicks on an enemy
    // This method will check if the player can attack and then perform the attack
    // TODO: You can add an animation trigger here if you have an attack animation
    public void Attack() {
        Debug.Log("Attacking");
        if(_currentTarget == null) 
            return;
        float cooldown = 1f / _runtime.AttackSpeed;
        
        if(Time.time - _lastAttackTime < cooldown) 
            return;
        
        RotateToTarget();
        
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, AttackRange, enemyLayer);
        foreach(Collider enemy in hitEnemies) {
            // Check if the enemy has an IAttackable component and call TakeDamage
            IAttackable attackable = enemy.GetComponent<IAttackable>();
            if(attackable != null)
            {
                int finalDamage = _runtime.Damage;
                bool crit = Random.value < _runtime.CritChance / 100f;

                if (crit)
                {
                    finalDamage = Mathf.RoundToInt(finalDamage * _runtime.CritDamage / 100f);
                }
                
                attackable.TakeDamage(finalDamage);
                Debug.Log($"[Melee Attack]: Attacked {enemy.name} for {finalDamage} damage.");
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
        float cooldown = 1f / _runtime.AttackSpeed;
        
        if(Time.time - _lastShootTime < cooldown) 
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
        ammoText.text = _currentAmmo.ToString(); // Update ammo text
    }

    public void OnDrawGizmosSelected() {
        if (_runtime == null)
            _runtime = GetComponent<PlayerRuntime>();

        if (_runtime == null)
            return;
        
        if(attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, _runtime.AttackRange);
    }
}
