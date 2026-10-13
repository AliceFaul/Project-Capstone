using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Gan tren Player. Nhan phim tuong ung (vi du: 1/2/3) -> dung artifact o slot tuong ung:
// kiem tra cooldown, lay diem/huong chuot,
// (neu co castTime) khoa di chuyen de cast roi mo khoa, chay lan luot cac Action.
// Cooldown tinh theo instanceId nen do artifact ra roi mac lai KHONG reset cooldown.
public class ArtifactController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController controller;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Animator animator;
    [Tooltip("Diem xuat phat projectile (vi du firePoint). De trong = vi tri nguoi choi nang len 1m.")]
    [SerializeField] private Transform castOrigin;
    
    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask obstacleLayer;

    private struct Cooldown
    {
        public float readyAt;
        public float duration;
    }
    
     private readonly Dictionary<string, Cooldown> _cooldowns = new Dictionary<string, Cooldown>();

     private InputSystem_Actions _input;
     private EquipmentManager _equipment;
     private PlayerRuntime _player;
     private PlayerBuffs _buffs;
     private bool _isCasting;
     
     public bool IsCasting => _isCasting;

     // (slot, artifact) - HUD co the dung de chay hieu ung ban phim.
     public event Action<int, ArtifactData> OnArtifactUsed;

     private void Awake()
     {
         _input = new InputSystem_Actions();
         
         if(controller == null) controller = GetComponent<PlayerController>();
         if(animator == null) animator = GetComponentInChildren<Animator>();
         if(mainCamera == null) mainCamera = Camera.main;

         _player = controller.PlayerRuntime;
         _buffs = controller.PlayerBuffs;
     }
     
     private void Start()
     {
         _equipment = EquipmentManager.Instance;
         if(_equipment == null) Debug.LogError($"[ArtifactController] EquipmentManager.Instance is missing.");
     }
     
     private void OnEnable()
     {
         _input.Player.Enable();
         _input.Player.Artifact1.performed += OnArtifact1;
         _input.Player.Artifact2.performed += OnArtifact2;
         _input.Player.Artifact3.performed += OnArtifact3;
     }

     private void OnDisable()
     {
         _input.Player.Artifact1.performed -= OnArtifact1;
         _input.Player.Artifact2.performed -= OnArtifact2;
         _input.Player.Artifact3.performed -= OnArtifact3;
         _input.Player.Disable();
     }

     private void OnArtifact1(InputAction.CallbackContext ctx) => TryActivate(0);
     private void OnArtifact2(InputAction.CallbackContext ctx) => TryActivate(1);
     private void OnArtifact3(InputAction.CallbackContext ctx) => TryActivate(2);

     public float GetCooldownRemaining(ArtifactInstance instance)
     {
         if (instance == null || !_cooldowns.TryGetValue(instance.instanceId, out var cooldown)) return 0f;
         return Mathf.Max(0f, cooldown.readyAt - Time.time);
     }

     public float GetCooldownRemaining(int slot)
     {
         return _equipment != null ? GetCooldownRemaining(_equipment.GetArtifact(slot)) : 0f;
     }

     // 0..1 (1 = vua bat dau cooldown) - dung cho vong/fill cooldown
     public float GetCooldownNormalized(int slot)
     {
         var instance = _equipment != null ? _equipment.GetArtifact(slot) : null;
         if (instance == null || !_cooldowns.TryGetValue(instance.instanceId, out var cooldown) || cooldown.duration <= 0f) return 0f;
         return Mathf.Clamp01(GetCooldownRemaining(instance) / cooldown.duration);
     }
     
     // Dung khi test: xoa het cooldown de thu lai ngay.
     public void ResetCooldown() => _cooldowns.Clear();

     public bool TryActivate(int slot)
     {
         if(_equipment == null || _isCasting) return false;

         var instance = _equipment.GetArtifact(slot);
         var data = _equipment.GetArtifactDefinition(instance);
         if(instance == null || data == null) return false;
         
         if(GetCooldownRemaining(instance) > 0f) return false;
         
         // Artifact co cast khoa di chuyen: khong cho dung khi dang danh / dang cast / dang bi khoa.
         if(data.LocksMovement && !controller.PlayerModifier.CanAttack) return false;
         if(!TryBuildContext(data, out var context)) return false; // khong nham duoc -> khong ton cooldown.

         _cooldowns[instance.instanceId] = new Cooldown { readyAt = Time.time + data.cooldown, duration = data.cooldown };
         OnArtifactUsed?.Invoke(slot, data);
         
         if(data.LocksMovement) StartCoroutine(CastRoutine(data, context));
         else Execute(data, context);
         return true;
     }

     private bool TryBuildContext(ArtifactData data, out ArtifactContext context)
     {
         Vector3 origin = castOrigin != null ? castOrigin.position : transform.position + Vector3.up;
         Vector3 aimPoint = transform.position;
         Vector3 aimDirection = transform.forward;

         if (data.targeting != ArtifactTargeting.Seft)
         {
             var mouse = Mouse.current;
             if (mouse == null || mainCamera == null)
             {
                 context = null;
                 return false;
             }
             
             Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
             if (!Physics.Raycast(ray, out var hit, 200f, groundLayer))
             {
                 context = null;
                 return false;
             }
             
             Vector3 flat = hit.point - transform.position;
             flat.y = 0f;

             aimDirection = flat.sqrMagnitude > 0.01f ? flat.normalized : transform.forward;
             aimPoint = hit.point;

             if (data.targeting == ArtifactTargeting.Point && flat.magnitude > data.maxRange)
             {
                 Vector3 clamped = transform.position + aimDirection * data.maxRange;
                 aimPoint = new Vector3(clamped.x, hit.point.y, clamped.z);
             }
         }

         context = new ArtifactContext
         {
            Data = data,
            Caster = _player,
            CasterTransform = transform,
            Origin = origin,
            AimPoint = aimPoint,
            AimDirection = aimDirection,
            Buffs = _buffs,
            Runner = this,
            EnemyLayer = enemyLayer,
            ObstacleLayer = obstacleLayer
         };
         return true;
     }

     // Khoa di chuyen + tan cong trong castTime, kich hoat Action o executeTime, xong la mo khoa va
     // di chuyen lai binh thuong (khong cho projectile/don danh trung dich).
     private IEnumerator CastRoutine(ArtifactData data, ArtifactContext context)
     {
         _isCasting = true;
         
         if(controller == null) yield break;
         controller.CmdCombatLocked(true);
         controller.Movement.Stop();
         
         if(data.targeting != ArtifactTargeting.Seft) transform.rotation = Quaternion.LookRotation(context.AimDirection);
         if(animator != null && !string.IsNullOrEmpty(data.animationTrigger)) animator.SetTrigger(data.animationTrigger);

         try
         {
             float executeAt = Mathf.Clamp(data.executeTime, 0f, data.castTime);
             if (executeAt > 0f) yield return new WaitForSeconds(executeAt);

             Execute(data, context);

             float remaining = data.castTime - executeAt;
             if (remaining > 0f) yield return new WaitForSeconds(remaining);
         }
         finally
         {
             if(controller != null) controller.CmdCombatLocked(false);
             _isCasting = false;
         }
         
     }

     private void Execute(ArtifactData data, ArtifactContext context)
     {
         foreach (var action in data.actions)
         {
             if(action == null) continue;

             try
             {
                 action.Execute(context);
             }
             catch (Exception e)
             {
                 Debug.LogException(e, this);
             }
         }
     }
}