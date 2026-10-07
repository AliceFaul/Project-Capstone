using System;
using System.Collections;
using Project.Capstone.Inventory;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerCombat : MonoBehaviour {
    [Header("Combat Setting")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    
    [Header("Equipment")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private EquipmentProgressConfig equipmentConfig;
    
    [Header("Status Effects (Test)")]
    public StatusEffect[] effects;
    
    [Header("Ammo Setting")]
    [SerializeField] private int maxAmmo = 15;
    [SerializeField] private TMP_Text ammoText;
    
    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayer;
    
    [Header("Impact feel")]
    [SerializeField] private float hitStopDuration = 0.05f;
    [SerializeField] private float hitStopTimeScale = 0.02f;
    private Coroutine _hitStopRoutine;
    
    // === CURRENT WEAPON ===
    private EquipmentInstance _currentMeleeWeapon;
    private EquipmentInstance _currentRangedWeapon;

    private Loadout _loadout;
    private PlayerController _controller;
    
    private Transform _currentTarget;
    private bool _activeCombatWindow;
    private int _currentAmmo;
    private Vector3 _shootPosition;
    
    private float _lastAttackTime;
    private float _lastShootTime;
    
    public Transform CurrentTarget => _currentTarget;
    public int CurrentAmmo => _currentAmmo;

    public float AttackRange { get; private set; }
    public float CurrentAttackSpeed { get; private set; }

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();

        foreach (var statusEffect in effects)
        {
            if(statusEffect == null) continue;
            statusEffect.OnStatusApplied += HandleStatusApplied;
        }
    }

    private void Start()
    {
        _currentAmmo = maxAmmo;
        if (ammoText != null) ammoText.text = _currentAmmo.ToString();
        
        _loadout = GetLoadout();
        
        if(_loadout == null)
        {
            Debug.LogError($"[PlayerCombat] Loadout was not found.");
        }
        else
        {
            _loadout.OnChanged += OnLoadoutChanged;
            _loadout.OnReloaded += OnLoadoutReloaded;
        }
        
        UpdateWeapons();
    }

    private void OnDestroy()
    {
        if (_loadout != null)
        {
            _loadout.OnChanged -= OnLoadoutChanged;
            _loadout.OnReloaded -= OnLoadoutReloaded;
        }
        
        foreach (var statusEffect in effects)
        {
            if(statusEffect == null) continue;
            statusEffect.OnStatusApplied -= HandleStatusApplied;
        }
    }

    public void SetTarget(Transform target)
    {
        _currentTarget = target;
        Debug.Log(target != null ? $"[PlayerCombat] Target: {target.name}" : $"[PlayerCombat] Target cleared.");
    }

    private Loadout GetLoadout()
    {
        // TODO: Need to initialize Loadout in PlayerDataConfig configuration.
        
        var playerData = StartupProcessor.Instance?.GetService<ConfigManager>().GetConfig<PlayerDataConfig>();
        if(playerData == null) return null;
        
        return playerData.Loadout;
    }

    private void HandleStatusApplied(IAttackable target, IStatusEffect statusEffect)
    {
        if(target is not MonoBehaviour mb) return;
        var statusUI = mb.GetComponentInChildren<StatusEffectUI>();
        statusUI?.ShowEffects(statusEffect);
    }

    // Call this method in the PlayerController when the player clicks on an enemy
    // This method will check if the player can attack and then perform the attack
    // TODO: You can add an animation trigger here if you have an attack animation
    public void CmdAttack() {
        if(_currentTarget == null) return;
        if(_currentMeleeWeapon == null) return;

        if (_activeCombatWindow)
        {
            RotateToTarget();
            _controller.AnimationHandler.CmdRequestAttacking();
            return;
        }
        
        if(!_controller.PlayerModifier.CanAttack) return;
        
        _controller.CmdCombatLocked(true);
        RotateToTarget();

        CurrentAttackSpeed = GetStat(_currentMeleeWeapon, BonusStat.AttackSpeed);
        _controller.AnimationHandler.CmdRequestAttacking();
        _controller.StateMachine.ChangeState(CharacterStateType.Attack);
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
        if(_currentMeleeWeapon == null) return;
        
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, AttackRange, enemyLayer);
        bool didHitAnything = false;
        
        foreach(Collider enemy in hitEnemies) {
            // Check if the enemy has an IAttackable component and call TakeDamage
            IAttackable attackable = enemy.GetComponent<IAttackable>();
            if(attackable != null)
            {
                if(!TryGetDefinition(_currentMeleeWeapon, out var definition)) return;
                var result = DamageCalculator.Calculate(_controller.PlayerRuntime, _currentMeleeWeapon, definition, equipmentConfig);
                attackable.TakeDamage(result.Damage, _controller.PlayerRuntime);
                
                if (effects != null)
                {
                    foreach (var t in effects)
                    {
                        if(t != null) Cast(t, attackable);
                    }
                }
                
                didHitAnything = true;
                Debug.Log($"[PlayerCombat] Attacked {enemy.name} for {result.Damage} damage.");
            } else {
                Debug.LogWarning($"[PlayerCombat] Enemy {enemy.name} does not implement IAttackable.");
            }
        }

        if (didHitAnything) Impact();
    }
    
    // Call this method in the PlayerController when the player right clicks
    // TODO: Add animation trigger here and ammo
    public void CmdShoot(Vector3 mousePosition)
    {
        if(_currentAmmo <= 0) return;
        if(_currentRangedWeapon == null) return;
        
        float attackSpeed = GetStat(_currentRangedWeapon, BonusStat.AttackSpeed);
        float cooldown = attackSpeed > 0f ? 1f / attackSpeed : float.MaxValue;
        
        if(Time.time - _lastShootTime < cooldown) return;
        
        Debug.Log($"[PlayerCombat] Shooting...");
        
        Vector3 direction = mousePosition - transform.position;
        direction.y = 0;
        if(direction.sqrMagnitude < 0.001f) return;
        
        transform.forward = direction.normalized;
        _shootPosition = mousePosition;
        CurrentAttackSpeed = attackSpeed;
        
        _controller.CmdCombatLocked(true);
        _controller.AnimationHandler.CmdAttackTrigger(1);
        _controller.StateMachine.ChangeState(CharacterStateType.Attack);
    }

    public void CmdSpawnProjectile()
    {
        Vector3 direction = _shootPosition - firePoint.position;
        direction.y = 0f;
        
        if(direction.sqrMagnitude < 0.001f) return;

        if(!TryGetDefinition(_currentRangedWeapon, out var definition)) return;
        var result = DamageCalculator.Calculate(_controller.PlayerRuntime, _currentRangedWeapon, definition, equipmentConfig);
        
        var projectile =  Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        var component = projectile.GetComponent<Projectile>();
        if (component == null)
        {
            Debug.LogError($"[PlayerCombat] Projectile prefab does not contain Projectile component.");
            return;
        }
        
        component.Initialize(direction, result.Damage);
        AdjustAmmo(-1);
        _lastShootTime = Time.time;
    }

    private void UpdateWeapons()
    {
        _currentMeleeWeapon = _loadout?.Melee;
        _currentRangedWeapon = _loadout?.Ranged;
        UpdateAttackRange();
        Debug.Log($"[PlayerCombat] Update weapons. Melee={_currentMeleeWeapon?.instanceId}, Ranged={_currentRangedWeapon?.instanceId}");
    }

    private void OnLoadoutReloaded() => UpdateWeapons();

    private void OnLoadoutChanged(EquipSlot slot, int index, IInventoryItem oldItem, IInventoryItem newItem)
    {
        switch (slot)
        {
            case EquipSlot.Melee:
                _currentMeleeWeapon = newItem as EquipmentInstance;
                UpdateAttackRange();
                break;
            case EquipSlot.Ranged:
                _currentRangedWeapon = newItem as EquipmentInstance;
                break;
        }
    }

    private bool TryGetDefinition(EquipmentInstance instance, out EquipmentData definition)
    {
        definition = null;
        
        if(instance == null) return false;
        if(!itemDatabase.TryGet(instance.definitionId, out var item)) return false;
        
        definition = item as EquipmentData;
        return definition != null;
    }

    private void UpdateAttackRange() => AttackRange = _currentMeleeWeapon == null ? 0f : GetStat(_currentMeleeWeapon, BonusStat.AttackRange);
    
    private float GetStat(EquipmentInstance instance, BonusStat stat)
    {
        if (instance == null) return 0f;

        if (itemDatabase == null)
        {
            Debug.LogError($"[PlayerCombat] ItemDatabase is not assigned.");
            return 0f;
        }

        if (!itemDatabase.TryGet(instance.definitionId, out var item))
        {
            Debug.LogError($"[PlayerCombat] Definition '{instance.definitionId}' does not exist.");
            return 0f;
        }

        if (item is not EquipmentData equipmentData)
        {
            Debug.LogError($"[PlayerCombat] Definition '{instance.definitionId}' is not EquipmentData.");
            return 0f;
        }
        
        return StatResolver.GetStat(instance, equipmentData, stat, equipmentConfig);
    }

    private void Cast(StatusEffect effect, IAttackable target)
    {
        effect.Apply(target);
        var mb = target as MonoBehaviour;

        if (effect.castVfx && mb)
            Instantiate(effect.castVfx, mb.transform.position + new Vector3(0f, 2f, 0f), Quaternion.identity);

        if (effect.runningVfx && mb)
        {
            var runningVfx = Instantiate(effect.runningVfx, mb.transform);
            Destroy(runningVfx, 3f);
        }
        
        // TODO: Add audio service
    }

    private void Impact()
    {
        if(CameraShake.Instance != null) CameraShake.Instance.ShakeCamera();

        if (_hitStopRoutine != null)
        {
            StopCoroutine(_hitStopRoutine);
            Time.timeScale = 1f;
        }
        _hitStopRoutine = StartCoroutine(HitStopCo());
    }

    private IEnumerator HitStopCo()
    {
        float previousTimeScale = Time.timeScale;
        Time.timeScale = hitStopTimeScale;

        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = previousTimeScale;
        _hitStopRoutine = null;
    }

    private void RotateToTarget()
    {
        Vector3 direction = _currentTarget.transform.position - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude < 0.01f) return;
        
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = lookRotation;
    }

    private void AdjustAmmo(int amount = 1)
    {
        _currentAmmo = Mathf.Clamp(_currentAmmo + amount, 0, maxAmmo);
        if(ammoText != null) ammoText.text = _currentAmmo.ToString();
    }
}