using System;
using System.Collections;
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
    
    // === CURRENT WEAPON ===
    private EquipmentData _currentMeleeWeapon;
    private EquipmentData _currentRangedWeapon;
    
    private Transform _currentTarget;
    public Transform CurrentTarget => _currentTarget;

    private bool _activeCombatWindow;
    private int _currentAmmo;
    private Vector3 _shootPosition;
    public int CurrentAmmo => _currentAmmo;
    
    private EquipmentManager _equipmentManager;
    private PlayerRuntime _runtime;
    private PlayerController _controller;

    public float AttackRange;
    
    private float _lastAttackTime;
    private float _lastShootTime;

    private void Awake()
    {
        _equipmentManager = EquipmentManager.Instance;
        _runtime = GetComponent<PlayerRuntime>();
        _controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        _currentAmmo = maxAmmo;
        if (ammoText != null)
        {
            ammoText.text = _currentAmmo.ToString();
        }
        
        if(_equipmentManager == null)
            return;

        _currentMeleeWeapon = _equipmentManager.GetCurrentEquipment(EquipmentType.MeleeWeapon);
        _currentRangedWeapon = _equipmentManager.GetCurrentEquipment(EquipmentType.RangedWeapon);
        AttackRange = _currentMeleeWeapon.attributes.attackRange;

        _equipmentManager.OnEquipmentChanged += UpdateWeapon;
    }

    private void OnDestroy()
    {
        if (_equipmentManager != null)
            _equipmentManager.OnEquipmentChanged -= UpdateWeapon;
    }

    public void SetTarget(Transform target)
    {
        _currentTarget = target;
        Debug.Log($"[PlayerCombat] Target: {target.name}");
    }

    // Call this method in the PlayerController when the player clicks on an enemy
    // This method will check if the player can attack and then perform the attack
    // TODO: You can add an animation trigger here if you have an attack animation
    public void CmdAttack() {
        Debug.Log("[PlayerCombat] Attacking");
        
        if(_currentTarget == null) 
            return;

        if (_activeCombatWindow)
        {
            RotateToTarget();
            _controller.AnimationHandler.CmdRequestAttacking();
            return;
        }
        
        if(!_controller.PlayerModifier.CanAttack)
            return;
        
        _controller.CmdCombatLocked(true);
        
        RotateToTarget();
        _controller.StateMachine.ChangeState(CharacterStateType.Attack);
        _controller.AnimationHandler.CmdRequestAttacking();
    }

    public void CmdActiveComboWindow(bool value) => _activeCombatWindow = value;
    
    public void CmdEndAttackingProcess()
    {
        _controller.CmdCombatLocked(false);
        _currentTarget = null;
        _controller.StateMachine.ChangeState(CharacterStateType.Locomotion);
    }
    
    // TODO: Add to Animation Event for exactly time
    public void CmdDealDamage()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, AttackRange, enemyLayer);
        foreach(Collider enemy in hitEnemies) {
            // Check if the enemy has an IAttackable component and call TakeDamage
            IAttackable attackable = enemy.GetComponent<IAttackable>();
            if(attackable != null)
            {
                var result = DamageCalculator.Calculate(_runtime, _currentMeleeWeapon);
                attackable.TakeDamage(result.Damage);
                CameraShake.Instance.ShakeCamera();
                CreateDamagePopup(enemy, result.Damage);
                Debug.Log($"[PlayerCombat] Attacked {enemy.name} for {result.Damage} damage.");
            } else {
                Debug.LogWarning($"Enemy {enemy.name} does not implement IAttackable.");
            }
        }
    }
    
    // Call this method in the PlayerController when the player right clicks
    // TODO: Add animation trigger here and ammo
    public void CmdShoot(Vector3 mousePosition)
    {
        if(_currentAmmo <= 0) 
            return;
        
        Debug.Log("[PlayerCombat] Shooting");
        
        if(Time.time - _lastShootTime < .7f) 
            return;
        
        Vector3 direction = mousePosition - transform.position;
        direction.y = 0;
        transform.forward = direction.normalized;
        
        _shootPosition = mousePosition;
        _controller.CmdCombatLocked(true);
        _controller.StateMachine.ChangeState(CharacterStateType.Attack);
        _controller.AnimationHandler.CmdAttackTrigger(1);
    }

    public void CmdSpawnProjectile()
    {
        Vector3 direction = _shootPosition - firePoint.position;
        direction.y = 0f;
        
        var result = DamageCalculator.Calculate(_runtime, _currentRangedWeapon);
        
        var  projectile =  Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        projectile.GetComponent<Projectile>().Initialize(direction, result.Damage);
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

    private void UpdateWeapon(EquipmentChangedEventArgs args)
    {
        switch (args.EquipmentType)
        {
            case EquipmentType.MeleeWeapon:
                _currentMeleeWeapon = args.NewEquipmentData;
                AttackRange = _currentMeleeWeapon.attributes.attackRange;
                break;
            case EquipmentType.RangedWeapon:
                _currentRangedWeapon = args.NewEquipmentData;
                break;
        }
    }

    private void CreateDamagePopup(Collider damageable, float damage)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError($"[PlayerCombat] UIManager instance is null");
            return;
        }
        
        var floatingTextService = UIManager.Instance.GetFloatingTextService();
        if (floatingTextService == null)
        {
            Debug.LogError($"[PlayerCombat] FloatingText service is null]");
        }

        string instanceId = $"dmg_{damageable.GetInstanceID()}_{Time.frameCount}_{Random.Range(0, 9999)}";
        var position = damageable.bounds.center + Vector3.up * (damageable.bounds.extents.y * 0.5f);
        
        floatingTextService?.Create("DamageText", 
                                            instanceId, 
                                            damage.ToString("0"), 
                                            position, 
                                            isMoving: true);
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